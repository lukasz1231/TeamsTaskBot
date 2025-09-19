using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models
{
    public class CommentModel
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public UserModel User { get; set; }
        public int LocalTaskId { get; set; }
        [ForeignKey("LocalTaskId")]
        public TaskModel Task { get; set; }
        public string Content { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
