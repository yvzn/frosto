using System;

namespace api.Data;

internal record Forecast(DateTime Date, decimal Minimum, decimal Maximum);
