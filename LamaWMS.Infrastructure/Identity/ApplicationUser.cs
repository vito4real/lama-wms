using Microsoft.AspNetCore.Identity;

namespace LamaWMS.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}