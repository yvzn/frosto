namespace admin.Models;

public class UserEntity : EntityBase
{
    public string? email { get; set; }
    public string? disabled { get; set; }
    public string? created { get; set; }
}
