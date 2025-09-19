using AdaptiveCards;

namespace Domain.Interfaces
{
    public interface IAdaptiveCardService
    {
        /// <summary>
        /// Creates a welcome card to introduce the bot.
        /// </summary>
        AdaptiveCard CreateWelcomeCard();

        /// <summary>
        /// Creates a card displaying a list of tasks with an associated action button.
        /// </summary>
        /// <param name="tasks">A collection of task names.</param>
        /// <param name="actionType">The type of action to perform on each task (e.g., 'comment', 'complete').</param>
        AdaptiveCard CreateTaskListCard(IEnumerable<string> tasks, string actionType);

        /// <summary>
        /// Creates a card with an input field for a user to leave a comment on a specific task.
        /// </summary>
        /// <param name="taskName">The name of the task the comment is for.</param>
        AdaptiveCard CreateCommentInputCard(int taskId, string taskName, string FromUserId);

        /// <summary>
        /// Creates a simple confirmation card with a message.
        /// </summary>
        /// <param name="message">The confirmation message to display.</param>
        AdaptiveCard CreateConfirmationCard(string message);

        /// <summary>
        /// Creates a card with options for a user to generate different types of reports.
        /// </summary>
        AdaptiveCard CreateReportOptionsCard();


        AdaptiveCard CreateMultiTaskCommentCard(IEnumerable<(int Id, string Title)> tasks, string FromUserId);
        AdaptiveCard CreateMultiTasksSelectionCard(IEnumerable<(int Id, string Title)> tasks, string FromUserId, string? Content = null, string? ActionType = null, string prompt = "Which tasks did you mean?");

        AdaptiveCard CreateUpdateTaskCard(IEnumerable<(int Id, string Title)> tasks, string fromUserId, string prompt = "Select task(s) to update and provide new values:");
        AdaptiveCard CreateTaskSelectionCard(
    List<(int Id, string Title)> tasks,
    bool onlyUserTasks,
    string fromUserId,
    string actionType);

        public AdaptiveCard CreateAddTimeToTaskCard(
        List<(int Id, string Title)> tasks,
        string fromUserId,
        bool needTime,
        bool needStart,
        bool needEnd,
        bool needPercent,
        string prompt);
    }
}
