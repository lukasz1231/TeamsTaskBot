using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models
{
    public class TaskModel
    {
        [Key]
        public int Id { get; set; }
        public string? TaskId { get; set; }
        public string Title { get; set; }
        public string NormalizedTitle { get; set; }
        [Column("StartDate")]
        public DateTimeOffset? StartDate { get; set; }
        [Column("DueDate")]
        public DateTimeOffset? DueDate { get; set; }
        public int PercentComplete { get; set; }
        public string PlanId { get; set; }
        public string BucketId { get; set; }

        public ICollection<UserModel> Users { get; set; } = new List<UserModel>();
        public ICollection<TimeEntryModel> TimeEntries { get; set; } = new List<TimeEntryModel>();
        public ICollection<CommentModel> Comments { get; set; } = new List<CommentModel>();

    }
}
