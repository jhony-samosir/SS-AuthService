using System.ComponentModel.DataAnnotations;

namespace SS.AuthService.Infrastructure.Services;

public class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    [EmailAddress]
    public string FromEmail { get; set; } = string.Empty;

    [Required]
    public string SmtpServer { get; set; } = "smtp.gmail.com";

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://localhost:7000";
}
