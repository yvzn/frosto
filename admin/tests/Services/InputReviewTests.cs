using admin.Services;

namespace admin.Tests.Services;

public class InputReviewTests
{
    private static readonly string[] KnownCountries = ["France", "United kingdom"];

    [Fact]
    public void GetNotes_WhenInputLooksComplete_ReturnsNoNote()
    {
        var notes = InputReview.GetNotes(null, 12_000, "France", KnownCountries);

        Assert.Empty(notes);
    }

    [Fact]
    public void GetNotes_WhenExtraInputProvided_ReturnsExtraInputNote()
    {
        var notes = InputReview.GetNotes("some text", 12_000, "France", KnownCountries);

        Assert.Equal([InputReview.ExtraInput], notes);
    }

    [Fact]
    public void GetNotes_WhenExtraInputIsBlank_ReturnsNoNote()
    {
        var notes = InputReview.GetNotes("   ", 12_000, "France", KnownCountries);

        Assert.Empty(notes);
    }

    [Fact]
    public void GetNotes_WhenDurationMissing_ReturnsNoDurationNote()
    {
        var notes = InputReview.GetNotes(null, null, "France", KnownCountries);

        Assert.Equal([InputReview.NoDuration], notes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(InputReview.MinDurationMs - 1)]
    public void GetNotes_WhenDurationShort_ReturnsShortDurationNote(int duration)
    {
        var notes = InputReview.GetNotes(null, duration, "France", KnownCountries);

        Assert.Equal([InputReview.ShortDuration], notes);
    }

    [Fact]
    public void GetNotes_WhenDurationAtThreshold_ReturnsNoNote()
    {
        var notes = InputReview.GetNotes(null, InputReview.MinDurationMs, "France", KnownCountries);

        Assert.Empty(notes);
    }

    [Theory]
    [InlineData("france")]
    [InlineData("  United Kingdom ")]
    public void GetNotes_WhenCountryKnownIgnoringCaseAndSpaces_ReturnsNoNote(string country)
    {
        var notes = InputReview.GetNotes(null, 12_000, country, KnownCountries);

        Assert.Empty(notes);
    }

    [Theory]
    [InlineData("xtvjtkybcy")]
    [InlineData("")]
    [InlineData(null)]
    public void GetNotes_WhenCountryUnknown_ReturnsUnrecognizedCountryNote(string? country)
    {
        var notes = InputReview.GetNotes(null, 12_000, country, KnownCountries);

        Assert.Equal([InputReview.UnrecognizedCountry], notes);
    }

    [Fact]
    public void GetNotes_WhenNoCountryList_SkipsCountryCheck()
    {
        var notes = InputReview.GetNotes(null, 12_000, "xtvjtkybcy");

        Assert.Empty(notes);
    }

    [Fact]
    public void GetNotes_WhenSeveralIssues_ReturnsAllNotesInOrder()
    {
        var notes = InputReview.GetNotes("text", 500, "xtvjtkybcy", KnownCountries);

        Assert.Equal([InputReview.ExtraInput, InputReview.ShortDuration, InputReview.UnrecognizedCountry], notes);
    }
}
