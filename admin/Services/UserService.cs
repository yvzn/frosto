using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        var normalizedEmail = email.Trim().ToLowerInvariant();
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

    private static User EntityToModel(UserEntity userEntity)
    {
        return new User
        {
            email = userEntity.email ?? string.Empty,
            PartitionKey = userEntity.PartitionKey ?? string.Empty,
            RowKey = userEntity.RowKey ?? string.Empty,
            Timestamp = userEntity.Timestamp,
        };
    }
}
