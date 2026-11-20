namespace admin.Services;

/// <summary>
/// Review notes shown next to form submissions (sign-ups, subscription checks),
/// to help decide which ones need a closer look before being processed.
/// </summary>
public static class InputReview
{
	public const string ExtraInput = "Extra input";
	public const string ShortDuration = "Short duration";
	public const string NoDuration = "No duration";
	public const string UnrecognizedCountry = "Unrecognized country";

	/// <summary>Submissions made faster than this, in milliseconds, get a note.</summary>
	public const int MinDurationMs = 3_000;

	public static IReadOnlyList<string> GetNotes(
		string? extraInput,
		int? duration,
		string? country = null,
		ICollection<string>? knownCountries = null)
	{
		var notes = new List<string>();

		if (!string.IsNullOrWhiteSpace(extraInput))
		{
			notes.Add(ExtraInput);
		}

		if (duration is null)
		{
			notes.Add(NoDuration);
		}
		else if (duration < MinDurationMs)
		{
			notes.Add(ShortDuration);
		}

		if (knownCountries is not null && !IsKnownCountry(country, knownCountries))
		{
			notes.Add(UnrecognizedCountry);
		}

		return notes;
	}

	private static bool IsKnownCountry(string? country, ICollection<string> knownCountries)
	{
		if (string.IsNullOrWhiteSpace(country))
		{
			return false;
		}

		var trimmed = country.Trim();
		return knownCountries.Any(known => string.Equals(known.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
	}
}
