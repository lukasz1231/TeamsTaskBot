using Domain.Entities.Enums;

namespace Domain.Entities.Dtos
{
    public class ParsedMessageDto
    {
        public ActionType Action { get; set; }
        public string? TaskName { get; set;  }
        public string? Content { get; set; }
        public string? Reply { get; set; }
        public int? PercentComplete { get; set; } // for StopTaskAsync
        public string? NewName { get; set; } // for UpdateTaskByName
        public double? Time { get; set; } // for AddTimeToTaskAsync
        public List<int>? ParsedNumbers { get; set; } // for ParsedNumbers action
        public DateTimeOffset? StartDate { get; set; } // for GetOverallReportAsync
        public DateTimeOffset? EndDate { get; set; } // for GetOverallReportAsync
        public List<string>? userIds { get; set; }
        public ParsedMessageDto(ActionType action, string? taskName=null, string? content = null, string? reply = null)
        {
            Action = action;
            TaskName = taskName;
            Content = content;
            Reply = reply;
        }
    }
}
