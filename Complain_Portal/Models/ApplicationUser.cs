using Microsoft.AspNetCore.Identity;

namespace Complain_Portal.Models
{
    public class ApplicationUser:IdentityUser
    {
        public string? FullName { get; set; }

        public string? Department { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; }
    }
}
