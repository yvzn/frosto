using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace batch.Services;

public static class Unsubscribe
{
	public static IDictionary<string, string> GetListUnsubscribeHeaders(string senderEmail, string recipientEmail, string? subscriptionId, string language)
	{
		var mailto = BuildMailtoHeader(senderEmail);
		var http = BuildHttpHeader(recipientEmail, subscriptionId, language);
		return new Dictionary<string, string>
		{
			{ "List-Unsubscribe", $"{mailto}, {http}" },
			{ "List-Unsubscribe-Post", "List-Unsubscribe=One-Click" }
		};
	}

	private static string BuildMailtoHeader(string senderEmail) => $"<mailto:{senderEmail}?subject=STOP&body=STOP>";

	private static string BuildHttpHeader(string recipientEmail, string? subscriptionId, string language)
	{
		var sub = recipientEmail;
		var sub_id = subscriptionId ?? "none";

		var privateKey = File.OpenRead(Path.Combine(ThisFunctionApp.WorkingDirectory, "dkim_private.pem"));
		using var rsa = RSA.Create();
		rsa.ImportFromPem(new StreamReader(privateKey).ReadToEnd().AsSpan());

		var signingKey = new RsaSecurityKey(rsa);
		var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

		var token = new JwtSecurityToken(
			claims:
			[
				new System.Security.Claims.Claim("sub", sub),
				new System.Security.Claims.Claim("sub_id", sub_id)
			],
			signingCredentials: credentials
		);

		var tokenHandler = new JwtSecurityTokenHandler();
		var tokenString = tokenHandler.WriteToken(token);

		return $"<{AppSettings.UnsubscribeUrl}?token={tokenString}&lang={language}>";
	}

}
