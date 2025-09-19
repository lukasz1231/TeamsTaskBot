using Domain.Entities.Models;
namespace Domain.Entities
{
    public class TaskReport
    {
        public int LocalTaskId { get; set; }
        public string TaskName { get; set; }
        public int TotalEntries { get; set; }
        public double TotalHours { get; set; }
        public string TotalHoursFormatted { get; set; }
        public int TotalUsers { get; set; }
        public string BestTimeUserId { get; set; }
        public string BestTimeUserName { get; set; }
        public string WorstTimeUserId { get; set; }
        public string WorstTimeUserName { get; set; }
        public double BestTime { get; set; }
        public string BestTimeFormatted { get; set; }
        public double WorstTime { get; set; }
        public string WorstTimeFormatted { get; set; }
        public List<TimeEntryModel> TimeEntries { get; set; }

    }
}
