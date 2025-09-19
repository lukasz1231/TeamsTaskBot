using Domain.Entities.Enums;

namespace Domain.Entities
{
    public static class ActionRoleAuthorization
    {
        public static readonly IReadOnlyDictionary<ActionType, string[]> RequiredRoles = new Dictionary<ActionType, string[]>
        {
            { ActionType.AddTask, new[] { "Task.Creator", "Admin" } },
            { ActionType.UpdateTaskByName, new[] { "Task.Editor", "Admin" } },
            { ActionType.DeleteTaskByName, new[] { "Task.Deleter", "Admin" } },
            { ActionType.CommentTaskByName, new[] { "Task.Commenter", "Admin" } },
            { ActionType.StartTaskByName, new[] { "Task.TimeTracker", "Admin" } },
            { ActionType.StopTaskByName, new[] { "Task.TimeTracker", "Admin" } },
            { ActionType.AddTimeToTaskByName, new[] { "Task.ManualTimeTracker", "Admin" } },
            { ActionType.GetUserReport, new[] { "Report.SeflViewer", "Report.AdminViewer", "Admin" } },
            { ActionType.GetTaskReport, new[] { "Report.SelfViewer", "Report.AdminViewer", "Admin" } },
            { ActionType.GetOverallReport, new[] { "Report.AdminViewer", "Admin" } },
            { ActionType.Smalltalk, new string[0] },
            { ActionType.Unknown, new string[0] }
        };

        public static bool HasPermission(List<string?> userRoles, ActionType action)
        {
            if (!RequiredRoles.ContainsKey(action))
            {
                // if action is not defined in the dictionary, allow it by default
                return true;
            }

            var requiredRoles = RequiredRoles[action];
            if (requiredRoles == null || !requiredRoles.Any())
            {
                return true;
            }

            return userRoles.Any(userRole => requiredRoles.Contains(userRole));
        }
    }
}