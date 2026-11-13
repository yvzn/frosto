const siteKey = process.env.SITE || 'fr';

export default async function () {
  const siteData = await import(`./sites/${siteKey}.json`, { assert: { type: 'json' } });
  return {
    ...siteData.default,
    env: {
      ...siteData.default.env,
      ...Object.fromEntries(
        Object.entries(process.env).filter(([key]) =>
          ['SIGNUP_URL', 'HEALTHCHECK_URL', 'SUPPORT_URL', 'CHECKSUBSCRIPTION_URL', 'UNSUBSCRIBE_URL'].includes(key)
        )
      )
    }
  };
}
