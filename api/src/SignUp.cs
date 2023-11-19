using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using System.Web;
using api.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System;

namespace api;

public static class SignUp
{
	[FunctionName("SignUp")]
	public static async Task<IActionResult> RunAsync(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sign-up")]
		HttpRequest request,
		[Table("location", Connection = "ALERTS_CONNECTION_STRING")]
		IAsyncCollector<LocationEntity> locationEntities,
		[Table("user", Connection = "ALERTS_CONNECTION_STRING")]
		IAsyncCollector<UserEntity> userEntities,
		[Table("signup", Connection = "ALERTS_CONNECTION_STRING")]
		IAsyncCollector<SignUpEntity> signUpEntities,
		ILogger log)
	{
		var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
		var requestParams = HttpUtility.ParseQueryString(requestBody);
		if (!IsValid(requestParams))
		{
			return new BadRequestResult();
		}

		var locationEntity = ParseLocationEntity(requestParams);

		var userEntity = ParseUserEntity(requestParams);
		userEntity.PartitionKey = locationEntity.RowKey;

		var signUpEntity = ParseSignUpEntity(requestParams);
		signUpEntity.RowKey = locationEntity.RowKey;

		await Task.WhenAll(
			locationEntities.AddAsync(locationEntity),
			userEntities.AddAsync(userEntity),
			signUpEntities.AddAsync(signUpEntity)
		);

		return new RedirectResult(AppSettings.SiteUrl + "sign-up-complete.html");
	}

	private static bool IsValid(NameValueCollection requestParams)
		=> string.Equals("true", requestParams["userConsent"], StringComparison.InvariantCultureIgnoreCase)
			&& requestParams["email"]?.Contains("@") is true
			&& requestParams["country"]?.Length is > 0
			&& requestParams["city"]?.Length is > 0;

	private static LocationEntity ParseLocationEntity(NameValueCollection requestParams)
		=> new LocationEntity
		{
			PartitionKey = requestParams["country"],
			RowKey = Guid.NewGuid().ToString(),
			city = requestParams["city"],
			country = requestParams["country"],
			users = requestParams["email"],
		};

	private static UserEntity ParseUserEntity(NameValueCollection requestParams)
		=> new UserEntity
		{
			PartitionKey = default,
			RowKey = Guid.NewGuid().ToString(),
			email = requestParams["email"],
			userConsent = requestParams["userConsent"],
		};

	private static SignUpEntity ParseSignUpEntity(NameValueCollection requestParams)
		=> new SignUpEntity
		{
			PartitionKey = requestParams["country"],
			RowKey = default,
			city = requestParams["city"],
			country = requestParams["country"],
		};
}
