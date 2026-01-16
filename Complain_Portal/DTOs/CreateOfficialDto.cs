using System.ComponentModel.DataAnnotations;

namespace Complain_Portal.DTOs
{

    public class CreateOfficialDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        [Required(ErrorMessage = "Phone Number is required")]
        public string PhoneNumber { get; set; }

        public string FullName { get; set; }

        // The new field!
        public string Department { get; set; }
    }

    public class UpdateOfficialDto
    {
        [Required]
        public string Id { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public string? Department { get; set; }
    }

}
