using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Models
{
    public class UserModel
    {
        [Key]
        public string UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string NormalizedDisplayName { get; set; }
        public string Email { get; set; } = string.Empty;

        public ICollection<TaskModel> Tasks { get; set; } = new List<TaskModel>();
        public ICollection<TimeEntryModel> TimeEntries { get; set; } = new List<TimeEntryModel>();
        public ICollection<CommentModel> Comments { get; set; } = new List<CommentModel>();
    }
}
