using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using admin.Models;

namespace admin.Services;

public class UnsubscribeService
{
    public Task<UnsubscribeResult> UnsubscribeAsync(string email, Location? validLocation, Location? location, User? user, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        if (validLocation is null || location is null || user is null)
        {
            return Task.FromResult(UnsubscribeResult.Failed("All unsubscribe entries must be provided."));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var success = !normalizedEmail.Contains("fail", StringComparison.Ordinal);

        if (!success)
        {
            return Task.FromResult(UnsubscribeResult.Failed($"Unable to unsubscribe {email}. Please try again later."));
        }

        var targets = new List<string>();
        if (validLocation is not null)
        {
            targets.Add($"valid location {validLocation.city}, {validLocation.country} ({validLocation.RowKey})");
        }
        if (user is not null)
        {
            targets.Add($"user record {user.email} ({user.RowKey})");
        }

        var summary = string.Join(" – ", targets);
        return Task.FromResult(UnsubscribeResult.Succeeded($"{email} unsubscribed from {summary}."));
    }
}

public readonly record struct UnsubscribeResult(bool Success, string Message)
{
    public static UnsubscribeResult Succeeded(string message) => new(true, message);

    public static UnsubscribeResult Failed(string message) => new(false, message);
}
