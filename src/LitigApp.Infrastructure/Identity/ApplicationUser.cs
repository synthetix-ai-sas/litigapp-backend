using Microsoft.AspNetCore.Identity;

namespace LitigApp.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? WhatsAppPhone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
