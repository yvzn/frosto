using Microsoft.Extensions.Caching.Memory;

namespace admin.Services;

/// <summary>
/// Provides geographical reference data (countries and timezones).
///
/// The lists returned by <see cref="GetCountryListAsync"/> and
/// <see cref="GetCommonTimezonesAsync"/> are built by merging a set of
/// hard-coded defaults with values stored in the <c>validlocation</c> database
/// table. Duplicates are removed using a case-insensitive comparison so each
/// entry appears exactly once.
///
/// Results are cached in memory after the first database fetch (lazy loading),
/// which avoids repeated round-trips on every request. The cache can be
/// explicitly invalidated via <see cref="InvalidateCache"/> when the
/// underlying data changes.
/// </summary>
public class GeographicalDataService(
	LocationService locationService,
	IMemoryCache cache,
	ILogger<GeographicalDataService> logger)
{
	/// <summary>Hard-coded country defaults used as the base list before merging database values.</summary>
	private static readonly string[] DefaultCountries =
		["France", "Algérie", "Belgique", "Canada", "Deutschland", "United kingdom", "United states of america"];

	/// <summary>Hard-coded timezone defaults used as the base list before merging database values.</summary>
	private static readonly string[] DefaultTimezones =
	[
		"Europe/Brussels",
		"Africa/Algiers",
		"America/Toronto",
		"America/Vancouver",
		"Europe/London",
		"America/New_York",
		"America/Chicago",
		"America/Denver",
		"America/Los_Angeles"
	];

	private const string CountryCacheKey = "GeographicalData:Countries";
	private const string TimezoneCacheKey = "GeographicalData:Timezones";

	private static readonly MemoryCacheEntryOptions CacheOptions = new()
	{
		AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
	};

	// Shared semaphore ensures only one database fetch occurs when the cache is empty,
	// preventing a thundering-herd of redundant queries on concurrent cache misses.
	private static readonly SemaphoreSlim LoadLock = new(1, 1);

	/// <summary>
	/// Returns a sorted, deduplicated list of countries built by merging the
	/// hard-coded defaults with the <c>country</c> field of every entry in the
	/// <c>validlocation</c> database table.
	///
	/// The result is cached in memory after the first database fetch. If the
	/// database is unavailable the hard-coded defaults are returned as a fallback.
	/// </summary>
	public async Task<ICollection<string>> GetCountryListAsync(CancellationToken cancellationToken = default)
	{
		if (cache.TryGetValue(CountryCacheKey, out ICollection<string>? cachedCountries) && cachedCountries is not null)
		{
			return cachedCountries;
		}

		await LoadAndCacheAsync(cancellationToken);

		// After loading, the cache is populated; retrieve the result.
		return cache.TryGetValue(CountryCacheKey, out ICollection<string>? countries) && countries is not null
			? countries
			: DefaultCountries;
	}

	/// <summary>
	/// Returns a sorted, deduplicated list of timezone identifiers built by
	/// merging the hard-coded defaults with the <c>timezone</c> field of every
	/// entry in the <c>validlocation</c> database table.
	///
	/// The result is cached in memory after the first database fetch. If the
	/// database is unavailable the hard-coded defaults are returned as a fallback.
	/// </summary>
	public async Task<ICollection<string>> GetCommonTimezonesAsync(CancellationToken cancellationToken = default)
	{
		if (cache.TryGetValue(TimezoneCacheKey, out ICollection<string>? cachedTimezones) && cachedTimezones is not null)
		{
			return cachedTimezones;
		}

		await LoadAndCacheAsync(cancellationToken);

		return cache.TryGetValue(TimezoneCacheKey, out ICollection<string>? timezones) && timezones is not null
			? timezones
			: DefaultTimezones;
	}

	/// <summary>
	/// Removes the cached country and timezone lists so that the next call to
	/// <see cref="GetCountryListAsync"/> or <see cref="GetCommonTimezonesAsync"/>
	/// reloads the data from the database.
	/// </summary>
	public void InvalidateCache()
	{
		cache.Remove(CountryCacheKey);
		cache.Remove(TimezoneCacheKey);
		logger.LogInformation("Geographical data cache invalidated");
	}

	/// <summary>
	/// Fetches all valid locations from the database once and populates both the
	/// country and timezone caches in a single round-trip. A static semaphore
	/// ensures only one fetch runs at a time, preventing redundant database queries
	/// on concurrent cache misses.
	/// </summary>
	private async Task LoadAndCacheAsync(CancellationToken cancellationToken)
	{
		await LoadLock.WaitAsync(cancellationToken);
		try
		{
			// Re-check cache after acquiring the lock; another request may have
			// already populated it while this one was waiting.
			if (cache.TryGetValue(CountryCacheKey, out _) && cache.TryGetValue(TimezoneCacheKey, out _))
			{
				return;
			}

			logger.LogDebug("Loading geographical data from database");

			var countries = new HashSet<string>(DefaultCountries, StringComparer.OrdinalIgnoreCase);
			var timezones = new HashSet<string>(DefaultTimezones, StringComparer.OrdinalIgnoreCase);

			try
			{
				var validLocations = await locationService.GetValidLocationsAsync(cancellationToken);

				foreach (var location in validLocations)
				{
					if (!string.IsNullOrWhiteSpace(location.country))
					{
						countries.Add(location.country.Trim());
					}

					if (!string.IsNullOrWhiteSpace(location.timezone))
					{
						timezones.Add(location.timezone.Trim());
					}
				}

				logger.LogDebug(
					"Merged geographical data: {CountryCount} countries, {TimezoneCount} timezones",
					countries.Count, timezones.Count);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to load geographical data from database; using hard-coded defaults");
			}

			cache.Set<ICollection<string>>(CountryCacheKey, [.. countries.OrderBy(c => c)], CacheOptions);
			cache.Set<ICollection<string>>(TimezoneCacheKey, [.. timezones.OrderBy(t => t)], CacheOptions);
		}
		finally
		{
			LoadLock.Release();
		}
	}
}
