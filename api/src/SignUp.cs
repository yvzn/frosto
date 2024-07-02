using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using System.Web;
using api.Data;
using System.Collections.Specialized;
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Azure.Data.Tables;

namespace api;

public class SignUp
{
	private readonly TableClient locationEntities;
	private readonly TableClient userEntities;
	private readonly TableClient signUpEntities;

	public SignUp(IAzureClientFactory<TableClient> azureClientFactory)
	{
		locationEntities = azureClientFactory.CreateClient("locationTableClient");
		userEntities = azureClientFactory.CreateClient("userTableClient");
		signUpEntities = azureClientFactory.CreateClient("signupTableClient");

#if DEBUG
		_ = Task.WhenAll(
			locationEntities.CreateIfNotExistsAsync(),
			userEntities.CreateIfNotExistsAsync(),
			signUpEntities.CreateIfNotExistsAsync()
		);
#endif
	}

	[Function("SignUp")]
	public async Task<IActionResult> RunAsync(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sign-up")]
		HttpRequest request)
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
			locationEntities.AddEntityAsync(locationEntity),
			userEntities.AddEntityAsync(userEntity),
			signUpEntities.AddEntityAsync(signUpEntity)
		);

		return new RedirectResult(AppSettings.SiteUrl + "sign-up-complete.html");
	}

	private static bool IsValid(NameValueCollection requestParams)
		=> string.Equals("true", requestParams["userConsent"], StringComparison.InvariantCultureIgnoreCase)
			&& requestParams["email"]?.Contains('@') is true
			&& requestParams["country"]?.Length is > 0
			&& requestParams["city"]?.Length is > 0;

	private static LocationEntity ParseLocationEntity(NameValueCollection requestParams)
		=> new ()
		{
			PartitionKey = requestParams["country"],
			RowKey = Guid.NewGuid().ToString(),
			city = requestParams["city"],
			zipCode = requestParams["zipCode"],
			country = requestParams["country"],
			users = requestParams["email"],
			lang = requestParams["lang"],
		};

	private static UserEntity ParseUserEntity(NameValueCollection requestParams)
		=> new ()
		{
			PartitionKey = default,
			RowKey = Guid.NewGuid().ToString(),
			email = requestParams["email"],
			userConsent = requestParams["userConsent"],
			lang = requestParams["lang"],
		};

	private static SignUpEntity ParseSignUpEntity(NameValueCollection requestParams)
		=> new ()
		{
			PartitionKey = requestParams["country"],
			RowKey = default,
			city = requestParams["city"],
			country = requestParams["country"],
		};
}
