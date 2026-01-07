using System.Collections.Generic;

namespace batch.Models;

internal class ScalewayApiEmailRequest
{
	public ScalewayApiIdentity? from { get; set; }
	public ICollection<ScalewayApiIdentity> to { get; set; } = [];
	public string? subject { get; set; }
	public string? project_id { get; set; }
	public string? text { get; set; }
	public string? html { get; set; }
	public ICollection<ScalewayApiAttachment> attachments { get; set; } = [];
	public ICollection<ScalewayApiHeader> additional_headers { get; set; } = [];
}

internal class ScalewayApiIdentity
{
	public string? name { get; set; }
	public string? email { get; set; }
}

internal class ScalewayApiHeader
{
	public string? key { get; set; }
	public string? value { get; set; }
}

internal class ScalewayApiAttachment
{
	public string? name { get; set; }
	public string? type { get; set; }
	public string? content { get; set; }
}
