using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Complain_Portal.Models
{
    public class Issue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        // (e.g., "/uploads/img1.jpg")
        public string? PhotoUrl { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string DepartmentCategory { get; set; }


        // Workflow tracking
        public IssueStatus Status { get; set; } = IssueStatus.Pending;
        public DateTime DateReported { get; set; } = DateTime.Now;
        public DateTime? DateResolved { get; set; }


        // Foreign key to ApplicationUser
        [Required]
        public string ReporterUserId { get; set; }

        [ForeignKey("ReporterUserId")]
        public ApplicationUser ReporterUser { get; set; }

        //Who is Fixing it? (Official) - Nullable initially!
        public string? AssignedOfficialId { get; set; }

        [ForeignKey("AssignedOfficialId")]
        public ApplicationUser? AssignedOfficial { get; set; }
    }
    public enum IssueStatus
    {
        Pending,
        Assigned,
        Resolved
    }
}
