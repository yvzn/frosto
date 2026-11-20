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
			result.Add(EntityToModel(signUpEntity));
		}

		return result;
	}

	public async Task<SignUp?> GetSignUpAsync(string id, CancellationToken cancellationToken)
	{
		var (partitionKey, rowKey) = id.ToKeys();

		var signUpEntity = await _signUpTableClient.GetEntityIfExistsAsync<SignUpEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);

		return signUpEntity.HasValue && signUpEntity.Value is not null ? EntityToModel(signUpEntity.Value) : null;
	}

	private static SignUp EntityToModel(SignUpEntity signUpEntity)
		=> new()
		{
			city = signUpEntity.city ?? "",
			country = signUpEntity.country ?? "",
			lang = signUpEntity.lang ?? "",
			temperatureUnit = signUpEntity.temperatureUnit,
			comment = signUpEntity.comment,
			duration = signUpEntity.duration,
			PartitionKey = signUpEntity.PartitionKey ?? "",
			RowKey = signUpEntity.RowKey ?? "",
			Timestamp = signUpEntity.Timestamp,
		};

	internal async Task<bool> DeleteSignUpAsync(string? id, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(id);

		var (partitionKey, rowKey) = id.ToKeys();

		var response = await _signUpTableClient.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken);

		return !response.IsError;
	}
}

