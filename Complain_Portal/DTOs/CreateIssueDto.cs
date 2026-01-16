using Complain_Portal.Models;

namespace Complain_Portal.DTOs
{
    public class CreateIssueDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string DepartmentCategory { get; set; }

        public IFormFile? Photo { get; set; }
    }
    // used for updating status
    public class UpdateStatusDto
    {
        public IssueStatus Status { get; set; }
    }

    // used for assigning officials
    public class AssignIssueDto
    {
        public string OfficialUserId { get; set; }
    }
}
