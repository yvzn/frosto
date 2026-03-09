using admin.Models;

namespace admin.Services;

public interface IMailTemplates
{
	(string subject, string htmlBody, string textBody) SubscriptionConfirmation(IList<Location> locations, string contactEmail);
	(string subject, string htmlBody, string textBody) UnsubscribeConfirmation();
}

public static class MailTemplates
{
	public static IMailTemplates For(string? lang) =>
		"en".Equals(lang, StringComparison.OrdinalIgnoreCase) ? English : French;

	public static readonly IMailTemplates English = new EnglishMailTemplates();
	public static readonly IMailTemplates French = new FrenchMailTemplates();

	private sealed class EnglishMailTemplates : IMailTemplates
	{
		public (string subject, string htmlBody, string textBody) SubscriptionConfirmation(
			IList<Location> locations,
			string contactEmail)
		{
			var locationListText = string.Join("\n", locations.Select(l => $"- {l.city}, {l.country}"));
			var locationListHtml = string.Concat(locations.Select(l =>
				$"<li>{System.Net.WebUtility.HtmlEncode(l.city)}, {System.Net.WebUtility.HtmlEncode(l.country)}</li>"));
			var contactEmailHtml = System.Net.WebUtility.HtmlEncode(contactEmail);

			return (
				subject: "Your Subscription Status to FrostAlert.net",
				htmlBody: $"""
					<html><body>
					<p>Hello,</p>
					<p>We are pleased to confirm that you are currently subscribed to frost alerts on FrostAlert.net for the following location(s):</p>
					<ul>{locationListHtml}</ul>
					<p>To ensure you receive your alerts, please add {contactEmailHtml} to your contact list.</p>
					<p>To unsubscribe, reply &ldquo;STOP&rdquo; to this message.</p>
					<p>Best regards,<br>Yvan from FrostAlert.net</p>
					</body></html>
					""",
				textBody: $"""
					Hello,

					We are pleased to confirm that you are currently subscribed to frost alerts on FrostAlert.net for the following location(s):

					{locationListText}

					To ensure you receive your alerts, please add {contactEmail} to your contact list.

					To unsubscribe, reply "STOP" to this message.

					Best regards,
					Yvan from FrostAlert.net
					"""
			);
		}

		public (string subject, string htmlBody, string textBody) UnsubscribeConfirmation() => (
			subject: "Your Unsubscription from FrostAlert.net",
			htmlBody: """
				<html><body>
				<p>Hello,</p>
				<p>Your unsubscription has been processed.<br>
				You may still receive a few alerts but these will be the last ones.</p>
				<p>Best regards,<br>Yvan from FrostAlert.net</p>
				</body></html>
				""",
			textBody: """
				Hello,

				Your unsubscription has been processed.
				You may still receive a few alerts but these will be the last ones.

				Best regards,
				Yvan from FrostAlert.net
				"""
		);
	}

	private sealed class FrenchMailTemplates : IMailTemplates
	{
		public (string subject, string htmlBody, string textBody) SubscriptionConfirmation(
			IList<Location> locations,
			string contactEmail)
		{
			var locationListText = string.Join("\n", locations.Select(l => $"- {l.city}, {l.country}"));
			var locationListHtml = string.Concat(locations.Select(l =>
				$"<li>{System.Net.WebUtility.HtmlEncode(l.city)}, {System.Net.WebUtility.HtmlEncode(l.country)}</li>"));
			var contactEmailHtml = System.Net.WebUtility.HtmlEncode(contactEmail);

			return (
				subject: "Votre abonnement à AlerteGelee.fr",
				htmlBody: $"""
					<html><body>
					<p>Bonjour,</p>
					<p>Nous sommes heureux de confirmer que vous êtes actuellement abonné aux alertes gelées sur AlerteGelee.fr pour le(s) villes(s) suivante(s)&nbsp;:</p>
					<ul>{locationListHtml}</ul>
					<p>Pour bien recevoir vos alertes, pensez à ajouter {contactEmailHtml} à votre liste de contacts.</p>
					<p>Pour vous désinscrire, répondez &laquo;&nbsp;STOP&nbsp;&raquo; à ce message.</p>
					<p>Cordialement,<br>Yvan de AlerteGelee.fr</p>
					</body></html>
					""",
				textBody: $"""
					Bonjour,

					Nous sommes heureux de confirmer que vous êtes actuellement abonné aux alertes gelées sur AlerteGelee.fr pour le(s) villes(s) suivante(s):

					{locationListText}

					Pour bien recevoir vos alertes, pensez à ajouter {contactEmail} à votre liste de contacts.

					Pour vous désinscrire, répondez "STOP" à ce message.

					Cordialement,
					Yvan de AlerteGelee.fr
					"""
			);
		}

		public (string subject, string htmlBody, string textBody) UnsubscribeConfirmation() => (
			subject: "Votre désinscription de AlerteGelee.fr",
			htmlBody: """
				<html><body>
				<p>Bonjour,</p>
				<p>Votre désinscription a bien été prise en compte.<br>
				Vous avez pu recevoir encore quelques alertes mais ce seront les dernières.</p>
				<p>Bonne continuation,</p>
				<p>Cordialement,<br>Yvan de AlerteGelee.fr</p>
				</body></html>
				""",
			textBody: """
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
