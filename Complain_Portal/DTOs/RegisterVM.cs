using System.ComponentModel.DataAnnotations;

namespace Complain_Portal.DTOs
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "User Name is required")]
        public string Username { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("Password", ErrorMessage = "The Password and Confirm Password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string FullName { get; set; }

        public string Role { get; set; } = "Citizen";
    }
}
