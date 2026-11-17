using System.IO;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Cryptography;

namespace admin.Services;

public class SmtpMailSender(IConfiguration configuration, IWebHostEnvironment environment, ILogger<SmtpMailSender> logger)
{
	private static readonly string ReplyToEncoded = "eXZhbkBhbGVydGVnZWxlZS5mcg==";

	public async Task<(bool success, string? error)> SendMailAsync(
		string to,
		string subject,
		string textBody,
		string? htmlBody = null,
		CancellationToken cancellationToken = default)
	{
		var connectionString = configuration.GetConnectionString("Smtp");
		if (!TryParseSmtpConnectionString(connectionString, out var smtpUrl, out var smtpLogin, out var smtpPassword))
		{
			logger.LogWarning("SMTP connection string is missing or invalid; cannot send mail");
			return (success: false, error: "SMTP configuration is missing");
		}

		var replyTo = new System.Net.Mail.MailAddress(Encoding.UTF8.GetString(Convert.FromBase64String(ReplyToEncoded)));

		var privateKeyPath = Path.Combine(environment.ContentRootPath, "dkim_private.pem");
		if (!File.Exists(privateKeyPath))
		{
			logger.LogWarning("DKIM private key not found at {Path}; cannot send mail", privateKeyPath);
			return (success: false, error: "DKIM private key not found");
		}

		await using var privateKeyStream = File.OpenRead(privateKeyPath);
		var signer = new DkimSigner(
			privateKeyStream,
			domain: replyTo.Host,
			selector: "frosto")
		{
			HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
			BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
			AgentOrUserIdentifier = $"@{replyTo.Host}",
			QueryMethod = "dns/txt",
		};

		var message = new MimeMessage();
		message.From.Add(new MailboxAddress("Yvan de Alertegelee.fr", replyTo.Address));
		message.To.Add(new MailboxAddress(to, to));
		message.Subject = subject;

		var builder = new BodyBuilder { TextBody = textBody, HtmlBody = htmlBody };
		message.Body = builder.ToMessageBody();
		message.Prepare(EncodingConstraint.SevenBit);

		var headers = new HeaderId[] { HeaderId.Date, HeaderId.From, HeaderId.Subject, HeaderId.To, HeaderId.MessageId };
		signer.Sign(message, headers);

		try
		{
			using var client = new SmtpClient();

			await client.ConnectAsync(smtpUrl, port: 465, SecureSocketOptions.SslOnConnect, cancellationToken);
			await client.AuthenticateAsync(smtpLogin, smtpPassword, cancellationToken);

			var response = await client.SendAsync(message, cancellationToken);

			await client.DisconnectAsync(true, cancellationToken);

			var success = response is not null && response.StartsWith("2.");
			logger.LogInformation("Mail sent to {Recipient}, response: {Response}", to, response);
			return (success, error: success ? null : response);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to send mail to {Recipient}", to);
			return (success: false, error: ex.Message);
		}
	}

	// Parses a connection string of the form "login:password@url"
	// The url is taken after the last '@'; the password may contain '@' or ':'
	// (login may not contain ':')
	internal static bool TryParseSmtpConnectionString(
		string? connectionString,
		out string url,
		out string login,
		out string password)
	{
		url = string.Empty;
		login = string.Empty;
		password = string.Empty;

		if (string.IsNullOrEmpty(connectionString))
			return false;

		var atIndex = connectionString.LastIndexOf('@');
		if (atIndex <= 0 || atIndex == connectionString.Length - 1)
			return false;

		url = connectionString[(atIndex + 1)..];

		var credentials = connectionString[..atIndex];
		var colonIndex = credentials.IndexOf(':');
		if (colonIndex <= 0)
			return false;

		login = credentials[..colonIndex];
		password = credentials[(colonIndex + 1)..];

		return !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password);
	}
}
