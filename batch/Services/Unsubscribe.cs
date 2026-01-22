using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography;
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

	public static IDictionary<string, string> GetListUnsubscribeHeaders(string senderEmail, string unsubscribeToken, string language)
	{
		var mailto = BuildMailtoHeader(senderEmail);
		var http = BuildHttpHeader(unsubscribeToken, language);
		return new Dictionary<string, string>
		{
			{ "List-Unsubscribe", $"{mailto}, {http}" },
			{ "List-Unsubscribe-Post", "List-Unsubscribe=One-Click" }
		};
	}

	private static string BuildMailtoHeader(string senderEmail)
		=> $"<mailto:{senderEmail}?subject=STOP&body=STOP>";

	private static string BuildHttpHeader(string unsubscribeToken, string language)
		=> $"<{BuildUnsubscribeUrl(unsubscribeToken, language)}>";

	internal static string BuildUnsubscribeUrl(string unsubscribeToken, string language)
	{
		return $"{AppSettings.UnsubscribeUrl}?token={unsubscribeToken}&lang={language}";
	}

	internal string BuildUnsubscribeToken(string recipientEmail, string? subscriptionId)
	{
		var sub = recipientEmail;
		var sub_id = subscriptionId ?? "none";

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
