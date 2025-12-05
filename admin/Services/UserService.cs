using System.Security.Cryptography;
using admin.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace admin.Services;

public class UserService(IAzureClientFactory<TableClient> azureClientFactory)
{
	private readonly TableClient _userTableClient = azureClientFactory.CreateClient("userTableClient");

	public async Task<IList<User>> FindUsersByEmailAsync(string email, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(email);

		var normalizedEmail = email.Trim();
		var matches = new List<User>();

		await foreach (var userEntity in _userTableClient.QueryAsync<UserEntity>(_ => true, cancellationToken: cancellationToken))
		{
			if (string.Equals(userEntity.email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
			{
				matches.Add(EntityToModel(userEntity));
			}
		}

		return matches;
	}

	public async Task<bool> DisableUserAsync(User user, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(user);

		var (partitionKey, rowKey) = user.Id.ToKeys();
		if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
		{
			return false;
		}

		var response = await _userTableClient.GetEntityAsync<UserEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var entity = response.Value;

		var timestamp = entity.Timestamp ?? DateTimeOffset.UtcNow;
		if (string.IsNullOrEmpty(entity.email))
		{
			entity.email = "disabled";
		}
		else
		{
			entity.email = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(entity.email))).ToLowerInvariant();
		}
		entity.disabled = "true";
		if (string.IsNullOrEmpty(entity.created))
		{
			entity.created = timestamp.ToString("O");
		}
		entity.PartitionKey ??= partitionKey;
		entity.RowKey ??= rowKey;

		await _userTableClient.UpdateEntityAsync(entity, entity.ETag, cancellationToken: cancellationToken);
		return true;
	}

	private static User EntityToModel(UserEntity userEntity)
	{
		return new User
		{
			email = userEntity.email ?? string.Empty,
			disabled = userEntity.disabled,
			created = userEntity.created,
			PartitionKey = userEntity.PartitionKey ?? string.Empty,
			RowKey = userEntity.RowKey ?? string.Empty,
			Timestamp = userEntity.Timestamp,
		};
	}
}
