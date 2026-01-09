using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace admin.Models;

public class Location : ModelBase
{
	[Required]
	[RegularExpression(@"^[^\d]+$")]
	[DisplayName("City")]
	public string city { get; set; } = "";
	[Required]
	[DisplayName("Country")]
	public string country { get; set; } = "";
	[Required]
	[RegularExpression(@"^-?[\d\.]+,\s*-?[\d\.]+$")]
	[DisplayName("Coordinates")]
	public string coordinates { get; set; } = "";
	[Required]
	[DisplayName("Users")]
	public string users { get; set; } = "";
	[DisplayName("UAT")]
	public bool uat { get; set; } = false;
	[DisplayName("Disabled")]
	public bool disabled { get; set; } = false;
	[DisplayName("Channel")]
	public string? channel { get; set; } = "";
	[DisplayName("Zip Code")]
	public string? zipCode { get; set; } = "";
	[DisplayName("Language")]
	public string? lang { get; set; } = "";
	[DisplayName("Timezone")]
	[RegularExpression(@"^(Africa|America|Antarctica|Arctic|Asia|Atlantic|Australia|Europe|Indian|Pacific|Etc)(/[A-Za-z0-9_\-\+]+){1,2}$")]
	public string? timezone { get; set; } = "";
	[DisplayName("Offset")]
	[RegularExpression(@"^[\+\-]([0-9]|[01][0-9]|2[0-3]):?([0-9]|[0-5][0-9])?$")]
	public string? offset { get; set; } = "";

	[HiddenInput]
	public string Id
	{
		get => GetId();
		set => SetId(value);
	}
}
