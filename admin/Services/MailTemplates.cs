using admin.Models;

namespace admin.Services;

public static class MailTemplates
{
	public static (string subject, string body) SubscriptionConfirmation(
		string lang,
		IList<Location> locations,
		string contactEmail)
	{
		var isEnglish = "en".Equals(lang, StringComparison.OrdinalIgnoreCase);
		var locationList = string.Join("\n", locations.Select(l => $"- {l.city}, {l.country}"));

		if (isEnglish)
		{
			return (
				subject: "Your Subscription Status to FrostAlert.net",
				body: $"""
				Hello,

				We are pleased to confirm that you are currently subscribed to frost alerts on FrostAlert.net for the following location(s):

				{locationList}

				To ensure you receive your alerts, please add {contactEmail} to your contact list.

				To unsubscribe, reply "STOP" to this message.

				Best regards,
				Yvan from FrostAlert.net
				"""
			);
		}

		return (
			subject: "Votre abonnement à AlerteGelee.fr",
			body: $"""
			Bonjour,

			Nous sommes heureux de confirmer que vous êtes actuellement abonné aux alertes gelées sur AlerteGelee.fr pour le(s) villes(s) suivante(s):

			{locationList}

			Pour bien recevoir vos alertes, pensez à ajouter {contactEmail} à votre liste de contacts.

			Pour vous désinscrire, répondez "STOP" à ce message.

			Cordialement,
			Yvan de AlerteGelee.fr
			"""
		);
	}

	public static (string subject, string body) UnsubscribeConfirmation(string lang)
	{
		var isEnglish = "en".Equals(lang, StringComparison.OrdinalIgnoreCase);

		if (isEnglish)
		{
			return (
				subject: "Your Unsubscription from FrostAlert.net",
				body: """
				Hello,

				Your unsubscription has been processed.
				You may still receive a few alerts but these will be the last ones.

				Best regards,
				Yvan from FrostAlert.net
				"""
			);
		}

		return (
			subject: "Votre désinscription de AlerteGelee.fr",
			body: """
			Bonjour,

			Votre désinscription a bien été prise en compte.
			Vous avez pu recevoir encore quelques alertes mais ce seront les dernières.

			Bonne continuation,

			Cordialement,
			Yvan de AlerteGelee.fr
			"""
		);
	}
}
