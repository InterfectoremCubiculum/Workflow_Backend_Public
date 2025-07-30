using System.ComponentModel.DataAnnotations;
using WorkflowTime.Features.UserManagment.Models;

namespace WorkflowTime.Features.Notifications.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public Guid UserId { get; set; }
        public virtual UserModel User { get; set; }

        public Notification(string? title, string? message, Guid userId)
        {
            UserId = userId;
            Title = title;
            Message = message;
            CreatedAt = DateTime.UtcNow;
            IsRead = false;
        }
    }
}
