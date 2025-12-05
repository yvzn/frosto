using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace admin.Models;

public class User : ModelBase
{
    [Required]
    [EmailAddress]
    public string email { get; set; } = string.Empty;

    public string? disabled { get; set; }
    public string? created { get; set; }

    [HiddenInput]
    public string Id
    {
        get => GetId();
        set => SetId(value);
    }
}
