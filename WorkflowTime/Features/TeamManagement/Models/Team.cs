using System.ComponentModel.DataAnnotations;
using WorkflowTime.Features.UserManagment.Models;

namespace WorkflowTime.Features.TeamManagement.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }
        public bool IsDeleted { get; set; } = false;
        public virtual ICollection<UserModel> Users { get; set; } = [];
    }
}
