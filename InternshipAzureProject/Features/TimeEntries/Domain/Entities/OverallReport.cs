using Domain.Entities.Models;
namespace Domain.Entities
{
    public class OverallReport
    {
        public int TotalEntries { get; set; }
        public double TotalHours { get; set; }
        public string TotalHoursFormatted { get; set; }
        public int TotalTasks { get; set; }
        public string BestTimeUserId { get; set; }
        public string BestTimeUserName { get; set; }
        public string WorstTimeUserId { get; set; }
        public string WorstTimeUserName { get; set; }
        public double BestTime { get; set; }
        public string BestTimeFormatted { get; set; }
        public double WorstTime { get; set; }
        public string WorstTimeFormatted { get; set; }

        public bool TimeTie => BestTime == WorstTime;

        public string BestTaskAmountUserId { get; set; }
        public string WorstTaskAmountUserId { get; set; }
        public int BestTaskAmount { get; set; }
        public int WorstTaskAmount { get; set; }

        public bool TaskAmountTie => BestTaskAmount == WorstTaskAmount;

        public List<TimeEntryModel> TimeEntries { get; set; }
    }
}
