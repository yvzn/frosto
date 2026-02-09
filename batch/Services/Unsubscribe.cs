using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using batch.Models;
using Microsoft.IdentityModel.Tokens;

namespace batch.Services;

public class Unsubscribe: IDisposable
{
	private readonly RSA signingAlgorithm;
	private readonly SigningCredentials signingCredentials;
	private bool disposedValue;

	public Unsubscribe()
	{
		signingAlgorithm = BuildRsa();
		signingCredentials = BuildSigningCredentials(signingAlgorithm);
	}

	private static RSA BuildRsa()
	{
		var privateKey = File.OpenRead(Path.Combine(ThisFunctionApp.WorkingDirectory, "dkim_private.pem"));
		var rsa = RSA.Create();
		rsa.ImportFromPem(new StreamReader(privateKey).ReadToEnd().AsSpan());
		return rsa;
	}

	private static SigningCredentials BuildSigningCredentials(RSA algorithm)
	{
		var signingKey = new RsaSecurityKey(algorithm);
		var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
		return credentials;
	}

	public static IDictionary<string, string> GetListUnsubscribeHeaders(
		string senderEmail,
		string unsubscribeToken,
		SingleRecipientNotification notification)
	{
		var mailto = BuildMailtoHeader(senderEmail);
		var http = BuildHttpHeader(unsubscribeToken, notification);
		return new Dictionary<string, string>
		{
			{ "List-Unsubscribe", $"{mailto}, {http}" },
			{ "List-Unsubscribe-Post", "List-Unsubscribe=One-Click" }
		};
	}

	private static string BuildMailtoHeader(string senderEmail)
		=> $"<mailto:{senderEmail}?subject=STOP&body=STOP>";

	private static string BuildHttpHeader(string unsubscribeToken, SingleRecipientNotification notification)
		=> $"<{BuildUnsubscribeUrl(unsubscribeToken, notification)}>";

	internal static string BuildUnsubscribeUrl(string unsubscribeToken, SingleRecipientNotification notification)
	{
		var uriBuilder = new UriBuilder(AppSettings.UnsubscribeUrl);

		var queryString = HttpUtility.ParseQueryString(string.Empty);
			queryString.Add("user", unsubscribeToken);
			queryString.Add("email", notification.to);
			queryString.Add("id", notification.rowKey);
			queryString.Add("lang", notification.lang ?? "fr");

		uriBuilder.Query = queryString.ToString();

		return uriBuilder.ToString();
	}

	internal string BuildUnsubscribeToken(SingleRecipientNotification notification)
	{
		var sub = notification.to;
		var sub_id = notification.rowKey ?? "none";

		var token = new JwtSecurityToken(
			claims:
			[
				new System.Security.Claims.Claim("sub", sub),
				new System.Security.Claims.Claim("sub_id", sub_id)
			],
			signingCredentials: signingCredentials
		);

		var tokenHandler = new JwtSecurityTokenHandler();
		var tokenString = tokenHandler.WriteToken(token);

		return tokenString;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				signingAlgorithm.Dispose();
			}

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
