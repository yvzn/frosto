using admin.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace admin.Services;

public class SignUpService(IAzureClientFactory<TableClient> azureClientFactory)
{
	private readonly TableClient _signUpTableClient = azureClientFactory.CreateClient("signupTableClient");

	public async Task<IList<SignUp>> GetNewSignUpsAsync(CancellationToken cancellationToken)
	{
		var result = new List<SignUp>();

		await foreach (var signUpEntity in _signUpTableClient.QueryAsync<SignUpEntity>(_ => true, cancellationToken: cancellationToken))
		{
			result.Add(new()
			{
				city = signUpEntity.city ?? "",
				country = signUpEntity.country ?? "",
				PartitionKey = signUpEntity.PartitionKey ?? "",
				RowKey = signUpEntity.RowKey ?? "",
				Timestamp = signUpEntity.Timestamp,
			});
		}

		return result;
	}

	internal async Task<bool> DeleteSignUpAsync(string? id, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(id);

		var (partitionKey, rowKey) = id.ToKeys();

		var response = await _signUpTableClient.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken);

		return !response.IsError;
	}
}

