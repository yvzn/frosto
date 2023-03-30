using System;

namespace api.Data;

internal record Forecast(DateOnly Date, decimal Minimum, decimal Maximum);
