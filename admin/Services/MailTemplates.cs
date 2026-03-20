using admin.Models;

namespace admin.Services;

public interface IMailTemplates
{
	(string subject, string htmlBody, string textBody) SubscriptionConfirmation(IList<Location> locations, string contactEmail, string? appLinkBase = null);
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
			string contactEmail,
			string? appLinkBase = null)
		{
			var locationListText = string.Join("\n", locations.Select(l => $"- {l.city}, {l.country}"));
			var locationListHtml = string.Concat(locations.Select(l =>
				$"<li>{System.Net.WebUtility.HtmlEncode(l.city)}, {System.Net.WebUtility.HtmlEncode(l.country)}</li>"));
			var contactEmailHtml = System.Net.WebUtility.HtmlEncode(contactEmail);

			var appSectionHtml = string.Empty;
			var appSectionText = string.Empty;
			if (!string.IsNullOrEmpty(appLinkBase))
			{
				var appLinkListHtml = string.Concat(locations.Select(l =>
					$"<li><a href=\"{System.Net.WebUtility.HtmlEncode(appLinkBase)}/app/weather-forecast/{Uri.EscapeDataString(l.PartitionKey)}/{Uri.EscapeDataString(l.RowKey)}\">{System.Net.WebUtility.HtmlEncode(l.city)}, {System.Net.WebUtility.HtmlEncode(l.country)}</a></li>"));
				var appLinkListText = string.Join("\n", locations.Select(l =>
					$"- {l.city}, {l.country}: {appLinkBase}/app/weather-forecast/{Uri.EscapeDataString(l.PartitionKey)}/{Uri.EscapeDataString(l.RowKey)}"));
				appSectionHtml = $"""
					<p>You can also visit the FrostAlert.net app to add your weather forecast alerts to your calendar:</p>
					<ul>{appLinkListHtml}</ul>
					<p>Please note that use of the app is entirely optional. You will continue to receive frost alerts by email, even if you do not use the app.</p>
					""";
				appSectionText = $"""

					You can also visit the FrostAlert.net app to add your weather forecast alerts to your calendar:

					{appLinkListText}

					Please note that use of the app is entirely optional. You will continue to receive frost alerts by email, even if you do not use the app.
					""";
			}

			return (
				subject: "Your Subscription Status to FrostAlert.net",
				htmlBody: $"""
					<html><body>
					<p>Hello,</p>
					<p>We are pleased to confirm that you are currently subscribed to frost alerts on FrostAlert.net for the following location(s):</p>
					<ul>{locationListHtml}</ul>
					{appSectionHtml}<p>To ensure you receive your alerts, please add {contactEmailHtml} to your contact list.</p>
					<p>To unsubscribe, reply &ldquo;STOP&rdquo; to this message.</p>
					<p>Best regards,<br>Yvan from FrostAlert.net</p>
					</body></html>
					""",
				textBody: $"""
					Hello,

					We are pleased to confirm that you are currently subscribed to frost alerts on FrostAlert.net for the following location(s):

					{locationListText}
					{appSectionText}
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
			string contactEmail,
			string? appLinkBase = null)
		{
			var locationListText = string.Join("\n", locations.Select(l => $"- {l.city}, {l.country}"));
			var locationListHtml = string.Concat(locations.Select(l =>
				$"<li>{System.Net.WebUtility.HtmlEncode(l.city)}, {System.Net.WebUtility.HtmlEncode(l.country)}</li>"));
			var contactEmailHtml = System.Net.WebUtility.HtmlEncode(contactEmail);

			var appSectionHtml = string.Empty;
			var appSectionText = string.Empty;
			if (!string.IsNullOrEmpty(appLinkBase))
			{
				var appLinkListHtml = string.Concat(locations.Select(l =>
					$"<li><a href=\"{System.Net.WebUtility.HtmlEncode(appLinkBase)}/app/weather-forecast/{Uri.EscapeDataString(l.PartitionKey)}/{Uri.EscapeDataString(l.RowKey)}\">{System.Net.WebUtility.HtmlEncode(l.city)}, {System.Net.WebUtility.HtmlEncode(l.country)}</a></li>"));
				var appLinkListText = string.Join("\n", locations.Select(l =>
					$"- {l.city}, {l.country}: {appLinkBase}/app/weather-forecast/{Uri.EscapeDataString(l.PartitionKey)}/{Uri.EscapeDataString(l.RowKey)}"));
				appSectionHtml = $"""
					<p>Vous pouvez également visiter l'application FrostAlert.net pour ajouter vos alertes météo à votre calendrier&nbsp;:</p>
					<ul>{appLinkListHtml}</ul>
					<p>Veuillez noter que l'utilisation de l'application est entièrement facultative. Vous continuerez à recevoir vos alertes gelées par e-mail, même si vous n'utilisez pas l'application.</p>
					""";
				appSectionText = $"""

					Vous pouvez également visiter l'application FrostAlert.net pour ajouter vos alertes météo à votre calendrier :

					{appLinkListText}

					Veuillez noter que l'utilisation de l'application est entièrement facultative. Vous continuerez à recevoir vos alertes gelées par e-mail, même si vous n'utilisez pas l'application.
					""";
			}

			return (
				subject: "Votre abonnement à AlerteGelee.fr",
				htmlBody: $"""
					<html><body>
					<p>Bonjour,</p>
					<p>Nous sommes heureux de confirmer que vous êtes actuellement abonné aux alertes gelées sur AlerteGelee.fr pour le(s) villes(s) suivante(s)&nbsp;:</p>
					<ul>{locationListHtml}</ul>
					{appSectionHtml}<p>Pour bien recevoir vos alertes, pensez à ajouter {contactEmailHtml} à votre liste de contacts.</p>
					<p>Pour vous désinscrire, répondez &laquo;&nbsp;STOP&nbsp;&raquo; à ce message.</p>
					<p>Cordialement,<br>Yvan de AlerteGelee.fr</p>
					</body></html>
					""",
				textBody: $"""
					Bonjour,

					Nous sommes heureux de confirmer que vous êtes actuellement abonné aux alertes gelées sur AlerteGelee.fr pour le(s) villes(s) suivante(s):

					{locationListText}
					{appSectionText}
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
