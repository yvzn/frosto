using System;

namespace batch.Models;

internal record Forecast(DateOnly Date, decimal Minimum, decimal Maximum);
