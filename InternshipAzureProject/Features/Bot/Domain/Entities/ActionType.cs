namespace Domain.Entities.Enums
{
    public enum ActionType
    {
        Unknown,
        AddTask,
        UpdateTaskByName,
        DeleteTaskByName,
        StartTaskByName,
        StopTaskByName,
        AddTimeToTaskByName,
        CommentTaskByName,
        GetOverallReport,
        GetUserReport,
        GetTaskReport,
        ParsedNumbers,
        Smalltalk
    }
}
