using Domain.Entities.Models;

namespace Domain.Entities
{
    public class UserReport
    {
        public string UserId { get; set; }
        public double TotalHours { get; set; }
        public string TotalHoursFormatted { get; set; }
        public int TotalEntries { get; set; }
        public int TotalTasks { get; set; }
        public List<TimeEntryModel> TimeEntries { get; set; }
    }
}
