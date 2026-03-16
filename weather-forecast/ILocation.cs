namespace weather;

#pragma warning disable IDE1006 // Naming Styles

public interface ILocation
{
	string? coordinates { get; }
	string? timezone { get; }
	string? weatherApiUrl { get; }
	double? minThreshold { get; }
	double? minTemperatureAdjustment { get; }
}

#pragma warning restore IDE1006 // Naming Styles
