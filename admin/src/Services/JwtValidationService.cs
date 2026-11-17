using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace admin.Services;

public class JwtValidationService(IWebHostEnvironment environment) : IDisposable
{
	private const string PublicKeyFileName = "dkim_public.pem";
	private readonly RSA _rsa = InitializeRsa(environment);
	private bool _disposed;

	private static RSA InitializeRsa(IWebHostEnvironment environment)
	{
		var pemPath = Path.Combine(environment.ContentRootPath, PublicKeyFileName);
		var pemContent = File.ReadAllText(pemPath);

		var rsa = RSA.Create();
		rsa.ImportFromPem(pemContent);
		return rsa;
	}

	public async Task<JwtValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(token))
		{
			return new JwtValidationResult { Error = "Token is empty." };
		}

		var handler = new JwtSecurityTokenHandler();

		if (!handler.CanReadToken(token))
		{
			return new JwtValidationResult { Error = "Token is not a valid JWT." };
		}

		try
		{
			var jwtToken = handler.ReadJwtToken(token);
			var algorithm = jwtToken.Header.Alg;

			// Validate token signature using the RSA key initialized in the constructor
			var validationParameters = new TokenValidationParameters
			{
				ValidateIssuer = false,
				ValidateAudience = false,
				ValidateLifetime = false,
				IssuerSigningKey = new RsaSecurityKey(_rsa)
			};

			var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

			// Extract claims - "sub" is normalized to NameIdentifier
			var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
			var subIdClaim = principal.FindFirst("sub_id");

			if (subClaim == null || subIdClaim == null)
			{
				return new JwtValidationResult
				{
					Error = "Token must contain both 'sub' and 'sub_id' claims.",
					SignatureValid = false,
					Algorithm = algorithm
				};
			}

			return new JwtValidationResult
			{
				SignatureValid = true,
				Algorithm = algorithm,
				Claims = new Dictionary<string, string>
				{
					{ "sub", subClaim.Value },
					{ "sub_id", subIdClaim.Value }
				}
			};
		}
		catch (SecurityTokenException ex)
		{
			return new JwtValidationResult
			{
				Error = $"JWT validation failed: {ex.Message}",
				SignatureValid = false
			};
		}
		catch (Exception ex)
		{
			return new JwtValidationResult
			{
				Error = $"Error processing token: {ex.Message}",
				SignatureValid = false
			};
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				_rsa?.Dispose();
			}
			_disposed = true;
		}
	}
}

public class JwtValidationResult
{
	public bool SignatureValid { get; set; }
	public string? Algorithm { get; set; }
	public Dictionary<string, string> Claims { get; set; } = [];
	public string? Error { get; set; }
}
