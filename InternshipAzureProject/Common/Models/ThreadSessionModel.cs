using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Models
{
    public class ThreadSessionModel
    {
        [Key]
        public string MessageThreadId { get; set; }
        public string ThreadId { get; set; }
        public DateTimeOffset LastActivity { get; set; }
    }
}
