using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Entities.Models;

namespace Domain.Entities
{
    public class PendingAction
    {
        public string userId;
        public List<string>? TargetUserIds { get; set; }
        public List<TaskModel>? tasks;
        public UpdateTaskDto? updatedTask; // for UpdateTaskByName
        public string? content; // for CommentTaskByName
        public int? percentComplete; // for StopTaskByName and AddTimeToTaskByName
        public double? time; // for AddTimeToTaskByName
        public ActionType actionType;
        public DateTimeOffset? startDate; // for GetTaskReport
        public DateTimeOffset? endDate; // for GetTaskReport
        public List<UserModel>? users; // for GetUserReport
        public PendingAction(string userId, List<TaskModel> tasks, UpdateTaskDto? updatedTask, string? content, int? percentComplete, double? time, ActionType actionType, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, List<UserModel>? users = null, List<string>? TargetUserIds = null)
        {
            this.userId = userId;
            this.tasks = tasks;
            this.updatedTask = updatedTask;
            this.content = content;
            this.percentComplete = percentComplete;
            this.time = time;
            this.actionType = actionType;
            this.startDate = startDate;
            this.endDate = endDate;
            this.users = users;
            this.TargetUserIds = TargetUserIds;
        }

    }
}
