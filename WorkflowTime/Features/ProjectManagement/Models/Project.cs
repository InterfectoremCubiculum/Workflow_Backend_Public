using System.ComponentModel.DataAnnotations;
using WorkflowTime.Features.UserManagment.Models;
namespace WorkflowTime.Features.ProjectManagement.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(255)]
        public required string Name { get; set; }
        public bool IsDeleted { get; set; } = false;
        public virtual ICollection<UserModel> Users { get; set; } = [];

    }
}
