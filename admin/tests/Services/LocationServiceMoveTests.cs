using admin.Services;
using Azure.Data.Tables;

namespace admin.Tests.Services;

public class LocationServiceMoveTests
{
	[Fact]
	public void ParseUsers_TrimsDeduplicatesAndPreservesSourceOrder()
	{
		var result = LocationService.ParseUsers(" Second@example.com,first@example.com, SECOND@example.com ,third@example.com ");

		Assert.Equal(["Second@example.com", "first@example.com", "third@example.com"], result);
	}

	[Fact]
	public void ValidateSelectedUsers_RejectsEmptySelection()
	{
		var exception = Assert.Throws<ArgumentException>(() =>
			LocationService.ValidateSelectedUsers(["one@example.com", "two@example.com"], []));

		Assert.Contains("at least one", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ValidateSelectedUsers_RejectsAllUsers()
	{
		var exception = Assert.Throws<ArgumentException>(() =>
			LocationService.ValidateSelectedUsers(
				["one@example.com", "two@example.com"],
				["TWO@example.com", "one@example.com"]));

		Assert.Contains("at least one user must remain", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ValidateSelectedUsers_AcceptsProperSubsetCaseInsensitively()
	{
		LocationService.ValidateSelectedUsers(
			["one@example.com", "two@example.com", "three@example.com"],
			["TWO@example.com"]);
	}

	[Fact]
	public void CloneEntity_CopiesUnknownPersistedProperties()
	{
		var source = new TableEntity("France", "source")
		{
			["users"] = "one@example.com,two@example.com",
			["futureProperty"] = 42,
			["nestedSetting"] = "preserved"
		};

		var clone = LocationService.CloneEntity(source);
		clone.RowKey = "destination";
		clone["users"] = "two@example.com";

		Assert.Equal("France", clone.PartitionKey);
		Assert.Equal("destination", clone.RowKey);
		Assert.Equal(42, clone["futureProperty"]);
		Assert.Equal("preserved", clone["nestedSetting"]);
		Assert.Equal("one@example.com,two@example.com", source["users"]);
	}
}
