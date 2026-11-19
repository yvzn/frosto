// Mock external services for local runs (started by the apphost when MOCKS is set).
//   GET  /open-meteo   fixed forecast, frost in the next 2-3 days (any coordinates)
//   POST /scaleway     Scaleway TEM stand-in: stores the mail, answers 200
//   POST /send-mail    custom mail API stand-in: stores the mail, answers 200
//   GET  /geocode, /timezone/byCoordinates/json   Azure Maps stand-ins (same place for any address)
//   SMTP on 465 (implicit TLS, any login)   stores the mail (only when MOCK_SMTP_PFX is set)
//   GET  /             inbox of the stored mails;  GET /mail/<id> shows headers + body (/raw: smtp raw message);  GET /healthz
// Dependencies: smtp-server, mailparser (npm install in this folder).

import { readFileSync } from 'node:fs';
import { createServer } from 'node:http';
import { simpleParser } from 'mailparser';
import { SMTPServer } from 'smtp-server';

const port = Number(process.env.PORT ?? 7090);
const mails = [];

const day = (offset) => {
  const d = new Date();
  d.setDate(d.getDate() + offset);
  return d.toISOString().slice(0, 10);
};

// [min, max] per day, today and the next 6 days: frost on days +1 .. +3
const dailyRange = [[4, 11], [-2, 6], [-4, 5], [-1, 7], [3, 10], [5, 12], [6, 13]];

function forecast() {
  const hourly = { time: [], soil_temperature_0cm: [], soil_temperature_6cm: [], apparent_temperature: [] };
  dailyRange.forEach(([min, max], i) => {
    for (let h = 0; h < 24; h++) {
      // coldest around 06:00, warmest around 15:00
      const t = min + ((max - min) * (1 - Math.cos(((h - 6) / 24) * 2 * Math.PI))) / 2;
      hourly.time.push(`${day(i)}T${String(h).padStart(2, '0')}:00`);
      hourly.soil_temperature_0cm.push(+(t - 1).toFixed(1));
      hourly.soil_temperature_6cm.push(+(t - 0.5).toFixed(1));
      hourly.apparent_temperature.push(+(t - 2).toFixed(1));
    }
  });
  return {
    hourly,
    daily: {
      time: dailyRange.map((_, i) => day(i)),
      temperature_2m_min: dailyRange.map(([min]) => min),
      temperature_2m_max: dailyRange.map(([, max]) => max),
      apparent_temperature_min: dailyRange.map(([min]) => min - 2),
      apparent_temperature_max: dailyRange.map(([, max]) => max - 2),
    },
  };
}


// Azure Maps stand-ins (admin, through AzureMapsEndpoint): same place for any address.
function geocode(query = '') {
  // "<zip> <city>, <country>"
  const [place = '', country = 'France'] = query.split(',').map((s) => s.trim());
  const locality = place.replace(/^\d+\s*/, '') || 'Paris';
  return {
    type: 'FeatureCollection',
    features: [{
      type: 'Feature',
      geometry: { type: 'Point', coordinates: [2.3522, 48.8566] },
      properties: {
        type: 'Locality',
        confidence: 'High',
        address: { locality, countryRegion: { name: country, ISO: 'FR' }, adminDistricts: [{ shortName: 'IDF', name: 'Île-de-France' }], formattedAddress: `${locality}, ${country}` },
      },
    }],
  };
}

function timezone() {
  return {
    Version: '2024a',
    ReferenceUtcTimestamp: new Date().toISOString(),
    TimeZones: [{
      Id: 'Europe/Paris',
      Aliases: [],
      Countries: [{ Name: 'France', Code: 'FR' }],
      Names: { ISO6391LanguageCode: 'en', Generic: 'Central European Time', Standard: 'Central European Standard Time', Daylight: 'Central European Summer Time' },
      ReferenceTime: { Tag: 'CET', StandardOffset: '01:00:00', DaylightSavings: '00:00:00', WallTime: new Date().toISOString(), PosixTzValidYear: new Date().getFullYear(), PosixTz: 'CET-1CEST,M3.5.0,M10.5.0/3' },
    }],
  };
}
const esc = (s) => String(s ?? '').replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);
const readBody = (req) => new Promise((resolve) => {
  let data = '';
  req.on('data', (c) => (data += c));
  req.on('end', () => resolve(data));
});
const send = (res, status, type, body) => {
  res.writeHead(status, { 'content-type': type });
  res.end(body);
};

// headers: text shown in the mail page. smtp: the real header block of the message received;
// scaleway / send-mail: rebuilt from the API request (headersNote says so).
function addMail(via, { to, subject, html, text, headers, headersNote, raw }) {
  const mail = { id: mails.length + 1, via, at: new Date().toISOString(), to, subject, html, text, headers, headersNote, raw };
  mails.push(mail);
  console.log(`[mock ${via}] mail #${mail.id} to ${to}: ${subject}`);
  return mail;
}

async function storeMail(req, res, via) {
  let body = {};
  try { body = JSON.parse(await readBody(req)); } catch { /* keep empty */ }
  // Scaleway: { to: [{email}], subject, html, text }; custom API: { to, subject, body }
  const to = Array.isArray(body.to) ? body.to.map((t) => t.email ?? t).join(', ') : body.to;
  // Scaleway also carries from, project_id and additional_headers [{key, value}] (List-Unsubscribe...)
  const identity = (i) => (i ? (i.name ? `${i.name} <${i.email}>` : i.email) : undefined);
  const lines = [
    body.from && `From: ${identity(body.from)}`,
    `To: ${to}`,
    `Subject: ${body.subject}`,
    body.project_id && `X-Scaleway-Project-Id: ${body.project_id}`,
    ...(body.additional_headers ?? []).map((h) => `${h.key}: ${h.value}`),
  ].filter(Boolean);
  const mail = addMail(via, {
    to,
    subject: body.subject,
    html: body.html ?? body.body,
    text: body.text,
    headers: lines.join('\n'),
    headersNote: `rebuilt from the ${via} API request (the real provider builds the final message headers)`,
  });
  send(res, 200, 'application/json', JSON.stringify({ id: mail.id }));
}

// SMTP stand-in (batch "smtp" channel and admin): implicit TLS on 465 like the real servers,
// any login accepted. Needs a certificate the clients trust for "localhost" (see README):
// MOCK_SMTP_PFX (path) and MOCK_SMTP_PFX_PASSWORD. Without them the SMTP part is not started.
function startSmtp() {
  const pfx = process.env.MOCK_SMTP_PFX;
  if (!pfx) return;
  const smtpPort = Number(process.env.MOCK_SMTP_PORT ?? 465);
  const smtp = new SMTPServer({
    secure: true,
    pfx: readFileSync(pfx),
    passphrase: process.env.MOCK_SMTP_PFX_PASSWORD,
    authOptional: true,
    onAuth: (auth, session, callback) => callback(null, { user: auth.username }),
    onData(stream, session, callback) {
      const chunks = [];
      stream.on('data', (chunk) => chunks.push(chunk));
      stream.on('error', callback);
      stream.on('end', () => {
        const raw = Buffer.concat(chunks);
        simpleParser(raw)
        .then((parsed) => {
          // the header block is everything before the first blank line (DKIM-Signature, List-Unsubscribe...)
          const text = raw.toString('utf8');
          const end = text.search(/\r?\n\r?\n/);
          const mail = addMail('smtp', {
            to: parsed.to?.text ?? session.envelope.rcptTo.map((r) => r.address).join(', '),
            subject: parsed.subject,
            html: parsed.html || undefined,
            text: parsed.text,
            headers: end >= 0 ? text.slice(0, end) : text,
            raw: text,
          });
          // real servers answer "2.0.0 Ok..."; admin and batch check that the reply starts with "2."
          callback(null, `2.0.0 Ok: queued as ${mail.id}`);
        })
        .catch(callback);
      });
    },
  });
  smtp.on('error', (e) => console.error(`[mock smtp] ${e.message}`));
  smtp.listen(smtpPort, () => console.log(`mock smtp (implicit TLS) on localhost:${smtpPort}`));
}

createServer(async (req, res) => {
  const url = new URL(req.url, `http://localhost:${port}`);
  const { pathname } = url;
  try {
    if (req.method === 'GET' && pathname === '/healthz') return send(res, 200, 'text/plain', 'ok');
    if (req.method === 'GET' && pathname === '/open-meteo') return send(res, 200, 'application/json', JSON.stringify(forecast()));
    if (req.method === 'GET' && pathname === '/geocode') return send(res, 200, 'application/json', JSON.stringify(geocode(url.searchParams.get('query'))));
    if (req.method === 'GET' && pathname.startsWith('/timezone/byCoordinates')) return send(res, 200, 'application/json', JSON.stringify(timezone()));
    if (req.method === 'POST' && pathname === '/scaleway') return await storeMail(req, res, 'scaleway');
    if (req.method === 'POST' && pathname === '/send-mail') return await storeMail(req, res, 'send-mail');
    if (req.method === 'GET' && pathname === '/') {
      const rows = mails.map((m) => `<tr><td>${m.id}</td><td>${esc(m.at)}</td><td>${m.via}</td><td>${esc(m.to)}</td><td><a href="/mail/${m.id}">${esc(m.subject)}</a></td></tr>`).join('');
      return send(res, 200, 'text/html; charset=utf-8',
        `<!doctype html><meta charset="utf-8"><title>Mock mails</title><h1>Mock mail inbox</h1><p>${mails.length} mail(s)</p><table border="1" cellpadding="4"><tr><th>#</th><th>at</th><th>via</th><th>to</th><th>subject</th></tr>${rows}</table>`);
    }
    const m = pathname.match(/^\/mail\/(\d+)(?:\/(body|raw))?$/);
    if (req.method === 'GET' && m) {
      const mail = mails[Number(m[1]) - 1];
      if (!mail) return send(res, 404, 'text/plain', 'not found');
      if (m[2] === 'body') return send(res, 200, 'text/html; charset=utf-8', mail.html ?? `<pre>${esc(mail.text)}</pre>`);
      if (m[2] === 'raw') return send(res, mail.raw ? 200 : 404, 'text/plain; charset=utf-8', mail.raw ?? 'no raw message (only smtp mails have one)');
      const note = mail.headersNote ? `<p><i>${esc(mail.headersNote)}</i></p>` : '';
      const raw = mail.raw ? ` &middot; <a href="/mail/${mail.id}/raw">raw message</a>` : '';
      return send(res, 200, 'text/html; charset=utf-8',
        `<!doctype html><meta charset="utf-8"><title>Mail #${mail.id}</title><p><a href="/">&larr; inbox</a>${raw}</p>` +
        `<h2>${esc(mail.subject)}</h2><h3>Headers (${mail.via})</h3>${note}` +
        `<pre style="white-space:pre-wrap;word-break:break-all;background:#f4f4f4;padding:8px">${esc(mail.headers ?? '(none)')}</pre>` +
        `<h3>Body</h3><iframe src="/mail/${mail.id}/body" style="width:100%;height:70vh;border:1px solid #ccc"></iframe>`);
    }
    send(res, 404, 'text/plain', 'not found');
  } catch (e) {
    send(res, 500, 'text/plain', String(e));
  }
}).listen(port, () => console.log(`mock services on http://localhost:${port}`));

startSmtp();
