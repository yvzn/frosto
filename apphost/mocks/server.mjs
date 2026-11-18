// Mock external services for local runs (started by the apphost when MOCKS is set).
//   GET  /open-meteo   fixed forecast, frost in the next 2-3 days (any coordinates)
//   POST /scaleway     Scaleway TEM stand-in: stores the mail, answers 200
//   POST /send-mail    custom mail API stand-in: stores the mail, answers 200
//   GET  /             inbox of the stored mails;  GET /mail/<id> shows one;  GET /healthz
// No dependency: plain node.

import { createServer } from 'node:http';

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

async function storeMail(req, res, via) {
  let body = {};
  try { body = JSON.parse(await readBody(req)); } catch { /* keep empty */ }
  // Scaleway: { to: [{email}], subject, html, text }; custom API: { to, subject, body }
  const to = Array.isArray(body.to) ? body.to.map((t) => t.email ?? t).join(', ') : body.to;
  const mail = { id: mails.length + 1, via, at: new Date().toISOString(), to, subject: body.subject, html: body.html ?? body.body, text: body.text };
  mails.push(mail);
  console.log(`[mock ${via}] mail #${mail.id} to ${to}: ${mail.subject}`);
  send(res, 200, 'application/json', JSON.stringify({ id: mail.id }));
}

createServer(async (req, res) => {
  const { pathname } = new URL(req.url, `http://localhost:${port}`);
  try {
    if (req.method === 'GET' && pathname === '/healthz') return send(res, 200, 'text/plain', 'ok');
    if (req.method === 'GET' && pathname === '/open-meteo') return send(res, 200, 'application/json', JSON.stringify(forecast()));
    if (req.method === 'POST' && pathname === '/scaleway') return await storeMail(req, res, 'scaleway');
    if (req.method === 'POST' && pathname === '/send-mail') return await storeMail(req, res, 'send-mail');
    if (req.method === 'GET' && pathname === '/') {
      const rows = mails.map((m) => `<tr><td>${m.id}</td><td>${esc(m.at)}</td><td>${m.via}</td><td>${esc(m.to)}</td><td><a href="/mail/${m.id}">${esc(m.subject)}</a></td></tr>`).join('');
      return send(res, 200, 'text/html; charset=utf-8',
        `<!doctype html><meta charset="utf-8"><title>Mock mails</title><h1>Mock mail inbox</h1><p>${mails.length} mail(s)</p><table border="1" cellpadding="4"><tr><th>#</th><th>at</th><th>via</th><th>to</th><th>subject</th></tr>${rows}</table>`);
    }
    const m = pathname.match(/^\/mail\/(\d+)$/);
    if (req.method === 'GET' && m) {
      const mail = mails[Number(m[1]) - 1];
      if (!mail) return send(res, 404, 'text/plain', 'not found');
      return send(res, 200, 'text/html; charset=utf-8', mail.html ?? `<pre>${esc(mail.text)}</pre>`);
    }
    send(res, 404, 'text/plain', 'not found');
  } catch (e) {
    send(res, 500, 'text/plain', String(e));
  }
}).listen(port, () => console.log(`mock services on http://localhost:${port}`));
