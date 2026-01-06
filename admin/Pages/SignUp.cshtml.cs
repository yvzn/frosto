using System.Linq;
using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;
using Azure;

namespace admin.Pages;

public class SignUpModel(
	LocationService locationService,
	GeocodingService geocodingService,
	SignUpService signUpService,
	IConfiguration configuration,
	ILogger<SignUpModel> logger) : PageModel
{
	[BindProperty]
	public Location ValidLocation { get; set; } = new();

	public string? ValidLocationExists { get; set; }

	public SelectList ChannelOptions { get; set; } = new(ChannelService.GetChannelList());

	public ICollection<string> CountryList { get; set; } = [];

	public ICollection<string> TimezoneList { get; set; } = [];

	public ICollection<Geocoding> GeocodingResults { get; private set; } = [];

	public string AzureMapsSubscriptionKey => _azureMapsSubscriptionKey;

	private readonly string _azureMapsSubscriptionKey = configuration.GetConnectionString("AzureMaps") ?? string.Empty;

	public async Task OnGetAsync(string id)
	{
		ValidLocation = await locationService.GetLocationAsync(id, HttpContext.RequestAborted) ?? new();

		await PopulateReferenceDataAsync(HttpContext.RequestAborted);
	}

	private static string Capitalize(string s)
	{
		var trimmed = s.Trim();

		if (string.IsNullOrWhiteSpace(trimmed))
		{
			return trimmed;
		}

		if (trimmed.Length == 1)
		{
			return trimmed.ToUpper();
		}

		return char.ToUpper(trimmed[0]) + trimmed[1..].ToLower();
	}

	public async Task<IActionResult> OnPostAsync()
	{
		if (!ModelState.IsValid)
		{
			await PopulateReferenceDataAsync(HttpContext.RequestAborted);
			return Page();
		}

		logger.LogInformation("Saving location {LocationId}", ValidLocation?.Id);
		await locationService.CreateValidLocationAsync(ValidLocation, HttpContext.RequestAborted);
		await signUpService.DeleteSignUpAsync(ValidLocation?.Id, HttpContext.RequestAborted);

		return RedirectToPage("./Index", new { m = $"Updated {ValidLocation?.city} ({ValidLocation?.RowKey})" });
	}

	public async Task<IActionResult> OnPostDiscardAsync()
	{
		logger.LogInformation("Discarding sign-up {LocationId}", ValidLocation?.Id);
		await signUpService.DeleteSignUpAsync(ValidLocation?.Id, HttpContext.RequestAborted);
		return RedirectToPage("./Index", new { m = $"Sign-Up discarded {ValidLocation?.city} ({ValidLocation?.RowKey})" });
	}

	private async Task PopulateReferenceDataAsync(CancellationToken cancellationToken)
	{
		ValidLocationExists = null;

		if (!string.IsNullOrWhiteSpace(ValidLocation?.city) && !string.IsNullOrWhiteSpace(ValidLocation.country))
		{
			var existingLocation = await locationService.FindValidLocationAsync(
				Capitalize(ValidLocation.city),
				Capitalize(ValidLocation.country),
				cancellationToken);
			ValidLocationExists = existingLocation?.Id;
		}

		CountryList = GeographicalDataService.GetCountryList();
		TimezoneList = GeographicalDataService.GetCommonTimezones();
		GeocodingResults = GetGeocodingResults(ValidLocation);
	}

	private Geocoding[] GetGeocodingResults(Location? location)
	{
		if (location is null)
		{
			return [];
		}

		if (string.IsNullOrWhiteSpace(location.city) || string.IsNullOrWhiteSpace(location.country))
		{
			return [];
		}

		try
		{
			return [.. geocodingService.GetGeocoding(location)];
		}
		catch (RequestFailedException ex)
		{
			logger.LogWarning(ex, "Geocoding lookup failed for {City}, {Country}", location.city, location.country);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unexpected error while retrieving geocoding for {City}, {Country}", location.city, location.country);
		}

		return [];
	}
}
