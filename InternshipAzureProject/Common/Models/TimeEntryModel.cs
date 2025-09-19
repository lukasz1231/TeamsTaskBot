using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models
{
    public class TimeEntryModel
    {
        [Key]
        public int Id { get; set; }

        public int LocalTaskId { get; set; }
        
        [ForeignKey("LocalTaskId")]
        public virtual TaskModel Task { get; set; }
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual UserModel User { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
