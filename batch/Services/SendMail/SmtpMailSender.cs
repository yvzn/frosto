using System;
using System.IO;
using System.Threading.Tasks;
using batch.Models;
using MimeKit.Cryptography;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Text;
using System.Threading;

namespace batch.Services.SendMail;

internal class SmtpMailSender : SingleRecipientMailSender
{
	private static readonly string ReplyTo = "eXZhbkBhbGVydGVnZWxlZS5mcg==";

	public override async Task<(bool success, string? error)> SendMailAsync(string recipient, Notification notification)
	{
		var privateKey = File.OpenRead(Path.Combine(ThisFunctionApp.WorkingDirectory, "dkim_private.pem"));
		var replyTo = new System.Net.Mail.MailAddress(Encoding.UTF8.GetString(Convert.FromBase64String(ReplyTo)));

		var signer = new DkimSigner(
			privateKey,
			domain: replyTo.Host,
			selector: "frosto")
		{
			HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
			BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
			AgentOrUserIdentifier = $"@{replyTo.Host}",
			QueryMethod = "dns/txt",
		};

		// Composing the whole email
		var message = new MimeMessage();
		message.From.Add(new MailboxAddress("Yvan de Alertegelee.fr", replyTo.Address));
		message.To.Add(new MailboxAddress(recipient, recipient));
		message.Subject = notification.subject;

		var unsubscribeToken = Unsubscribe.BuildUnsubscribeToken(recipient, notification.rowKey);

		var listUnsubscribeHeaders = Unsubscribe.GetListUnsubscribeHeaders(replyTo.Address, unsubscribeToken, notification.lang ?? "fr");
		foreach (var header in listUnsubscribeHeaders)
			message.Headers.Add(header.Key, header.Value);

		var unsubscribeUrl = Unsubscribe.BuildUnsubscribeUrl(unsubscribeToken, notification.lang ?? "fr");
		var unsubscribeLink = HtmlFormatter.FormatUnsubscribeLink(unsubscribeUrl);

		var builder = new BodyBuilder
		{
			TextBody = notification.raw,
			HtmlBody = notification.body?.Replace(HtmlFormatter.unsubscribeLinkPlaceholder, unsubscribeUrl)
		};

		message.Body = builder.ToMessageBody();
		message.Prepare(EncodingConstraint.SevenBit);

		var headers = new HeaderId[] { HeaderId.Date, HeaderId.From, HeaderId.ListUnsubscribe, HeaderId.Subject, HeaderId.To, HeaderId.MessageId };

		signer.Sign(message, headers);

		// Sending the email
		async ValueTask<string?> sendmail(CancellationToken cancellationToken)
		{
			using var client = new SmtpClient();

			await client.ConnectAsync(AppSettings.SmtpUrl, port: 465, SecureSocketOptions.SslOnConnect, cancellationToken);
			await client.AuthenticateAsync(AppSettings.SmtpLogin, AppSettings.SmtpPassword, cancellationToken);

			var response = default(string);
			response = await client.SendAsync(message, cancellationToken);

			await client.DisconnectAsync(true, cancellationToken);

			return response;
		}

		var response = await RetryStrategy.For.Smtp.ExecuteAsync(sendmail);

		return (success: response is not null && response.StartsWith("2."), error: response);
	}
}
