using System.ComponentModel.DataAnnotations;

namespace Complain_Portal.DTOs
{
    public class LoginVM
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
