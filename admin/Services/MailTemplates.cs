using admin.Models;
using System.Text;

namespace admin.Services;

public interface IMailTemplates
{
	(string subject, string htmlBody, string textBody) SubscriptionConfirmation(IList<Location> locations, string contactEmail, bool addLinkToApp = false, bool includeBodyTag = true);
	(string subject, string htmlBody, string textBody) UnsubscribeConfirmation();
}

public class MailTemplates(IConfiguration configuration)
{
	public IMailTemplates For(string? lang) =>
		"en".Equals(lang, StringComparison.OrdinalIgnoreCase) ? English : French;

	private readonly IMailTemplates English = new EnglishMailTemplates(configuration["SiteEnUrl"]);
	private readonly IMailTemplates French = new FrenchMailTemplates(configuration["SiteFrUrl"]);

	private sealed class EnglishMailTemplates(string? siteEnUrl) : IMailTemplates
	{
		public (string subject, string htmlBody, string textBody) SubscriptionConfirmation(
			IList<Location> locations,
			string contactEmail,
			bool addLinkToApp = false,
			bool includeBodyTag = true)
		{
			var contactEmailHtml = System.Net.WebUtility.HtmlEncode(contactEmail);

			var html = new StringBuilder();
			if (includeBodyTag) html.AppendLine("<html><body>");
			html.AppendLine("<p>Hello,</p>");
			html.AppendLine("<p>We are pleased to confirm that you are currently subscribed to frost alerts for the following location(s):</p>");
			html.Append("<ul>");
			foreach (var l in locations)
				html.Append($"<li>{System.Net.WebUtility.HtmlEncode(l.city)}, {System.Net.WebUtility.HtmlEncode(l.country)}</li>");
			html.AppendLine("</ul>");
			html.AppendLine($"<p>To ensure you receive your alerts, please add {contactEmailHtml} to your contact list.</p>");
			if (addLinkToApp)
			{
				html.AppendLine("<p>You can also optionally visit our app to add alerts to your calendar:</p>");
				html.Append("<ul>");
				foreach (var l in locations)
					html.Append($"<li><a href=\"{siteEnUrl}app/weather-forecast/{Uri.EscapeDataString(l.PartitionKey)}/{Uri.EscapeDataString(l.RowKey)}\">{System.Net.WebUtility.HtmlEncode(l.city)}, {System.Net.WebUtility.HtmlEncode(l.country)}</a></li>");
				html.AppendLine("</ul>");
			}
			html.AppendLine("<p>To unsubscribe, reply &ldquo;STOP&rdquo; to this message.</p>");
			html.Append("<p>Best regards,<br>Yvan from FrostAlert.net</p>");
			if (includeBodyTag) html.AppendLine().Append("</body></html>");

			var text = new StringBuilder();
			text.AppendLine("Hello,");
			text.AppendLine();
			text.AppendLine("We are pleased to confirm that you are currently subscribed to frost alerts for the following location(s):");
			text.AppendLine();
			foreach (var l in locations)
				text.AppendLine($"- {l.city}, {l.country}");
			text.AppendLine();
			text.AppendLine($"To ensure you receive your alerts, please add {contactEmail} to your contact list.");
			if (addLinkToApp)
			{
				text.AppendLine();
				text.AppendLine("You can also optionally visit our app to add alerts to your calendar:");
				text.AppendLine();
				foreach (var l in locations)
					text.AppendLine($"- {l.city}, {l.country}: {siteEnUrl}app/weather-forecast/{Uri.EscapeDataString(l.PartitionKey)}/{Uri.EscapeDataString(l.RowKey)}");
			}
			text.AppendLine();
			text.AppendLine("To unsubscribe, reply \"STOP\" to this message.");
			text.AppendLine();
			text.AppendLine("Best regards,");
			text.Append("Yvan from FrostAlert.net");

			return (
				subject: "Your Subscription Status to FrostAlert.net",
				htmlBody: html.ToString(),
				textBody: text.ToString()
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

	private sealed class FrenchMailTemplates(string? siteFrUrl) : IMailTemplates
	{
		public (string subject, string htmlBody, string textBody) SubscriptionConfirmation(
			IList<Location> locations,
			string contactEmail,
			bool addLinkToApp = false,
			bool includeBodyTag = true)
		{
			var contactEmailHtml = System.Net.WebUtility.HtmlEncode(contactEmail);

			var html = new StringBuilder();
			if (includeBodyTag) html.AppendLine("<html><body>");
			html.AppendLine("<p>Bonjour,</p>");
			html.AppendLine("<p>Nous sommes heureux de confirmer que vous êtes actuellement abonné aux alertes gelées pour le(s) villes(s) suivante(s)&nbsp;:</p>");
			html.Append("<ul>");
			foreach (var l in locations)
				html.Append($"<li>{System.Net.WebUtility.HtmlEncode(l.city)}, {System.Net.WebUtility.HtmlEncode(l.country)}</li>");
			html.AppendLine("</ul>");
			html.AppendLine($"<p>Pour bien recevoir vos alertes, pensez à ajouter {contactEmailHtml} à votre liste de contacts.</p>");
			if (addLinkToApp)
			{
				html.AppendLine("<p>Vous pouvez également visiter notre application pour ajouter les alertes à votre calendrier (optionnel)&nbsp;:</p>");
				html.Append("<ul>");
				foreach (var l in locations)
					html.Append($"<li><a href=\"{siteFrUrl}app/weather-forecast/{Uri.EscapeDataString(l.PartitionKey)}/{Uri.EscapeDataString(l.RowKey)}\">{System.Net.WebUtility.HtmlEncode(l.city)}, {System.Net.WebUtility.HtmlEncode(l.country)}</a></li>");
				html.AppendLine("</ul>");
			}
			html.AppendLine("<p>Pour vous désinscrire, répondez &laquo;&nbsp;STOP&nbsp;&raquo; à ce message.</p>");
			html.Append("<p>Cordialement,<br>Yvan de AlerteGelee.fr</p>");
			if (includeBodyTag) html.AppendLine().Append("</body></html>");

			var text = new StringBuilder();
			text.AppendLine("Bonjour,");
			text.AppendLine();
			text.AppendLine("Nous sommes heureux de confirmer que vous êtes actuellement abonné aux alertes gelées pour le(s) villes(s) suivante(s):");
			text.AppendLine();
			foreach (var l in locations)
				text.AppendLine($"- {l.city}, {l.country}");
			text.AppendLine();
			text.AppendLine($"Pour bien recevoir vos alertes, pensez à ajouter {contactEmail} à votre liste de contacts.");
			if (addLinkToApp)
			{
				text.AppendLine();
				text.AppendLine("Vous pouvez également visiter notre application pour ajouter les alertes à votre calendrier (optionnel) :");
				text.AppendLine();
				foreach (var l in locations)
					text.AppendLine($"- {l.city}, {l.country}: {siteFrUrl}app/weather-forecast/{Uri.EscapeDataString(l.PartitionKey)}/{Uri.EscapeDataString(l.RowKey)}");
			}
			text.AppendLine();
			text.AppendLine("Pour vous désinscrire, répondez \"STOP\" à ce message.");
			text.AppendLine();
			text.AppendLine("Cordialement,");
			text.Append("Yvan de AlerteGelee.fr");

			return (
				subject: "Votre abonnement à AlerteGelee.fr",
				htmlBody: html.ToString(),
				textBody: text.ToString()
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
