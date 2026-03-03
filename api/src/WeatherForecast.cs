using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using api.Data;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace api;

public class WeatherForecast
{
	private readonly TableClient validLocationEntities;

	public WeatherForecast(IAzureClientFactory<TableClient> azureClientFactory)
	{
		validLocationEntities = azureClientFactory.CreateClient("validlocationTableClient");

#if DEBUG
		_ = Task.WhenAll(
			validLocationEntities.CreateIfNotExistsAsync()
		);
#endif
	}

    [Function("WeatherForecast")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")]
        HttpRequest request,
        ILogger<WeatherForecast> logger,
        CancellationToken cancellationToken)
    {
        var queryParameters = request.Query;
        if (!IsValid(queryParameters))
        {
			logger.LogWarning("Failed to decode location p={PartitionKey} r={RowKey}", queryParameters["p"], queryParameters["r"]);
            return new BadRequestResult();
        }

        var (PartitionKey, RowKey) = Decode(queryParameters);

		var location = await validLocationEntities.GetEntityAsync<LocationEntity>(PartitionKey, RowKey, cancellationToken: cancellationToken);

		var response = location.GetRawResponse();
		if (response.IsError)
		{
			logger.LogError("Failed to get location {PartitionKey} {RowKey} {StatusCode} [{ResponseContent}]", PartitionKey, RowKey, response.Status, Encoding.UTF8.GetString(response.Content));
			return new BadRequestResult();
		}

    }

	private static (string PartitionKey, string RowKey) Decode(IQueryCollection queryParameters)
		=> (
			queryParameters["p"]!,
			queryParameters["r"]!
        );

	private static bool IsValid(IQueryCollection queryParameters)
		=> !string.IsNullOrWhiteSpace(queryParameters["p"])
			&& !string.IsNullOrWhiteSpace(queryParameters["r"]);
}
