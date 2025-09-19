using Domain.Interfaces;
using System.Collections.Generic;


namespace Infrastructure.Services
{
    using AdaptiveCards;

    public class AdaptiveCardService : IAdaptiveCardService
    {
        /// <summary>
        /// Creates a welcome card to introduce the bot.
        /// The card includes a title, a brief text block, and an image.
        /// </summary>
        public AdaptiveCard CreateWelcomeCard()
        {
            var card = new AdaptiveCard("1.2");

            card.Body.Add(new AdaptiveTextBlock
            {
                Text = "Welcome to the Task Management Bot! 👋",
                Size = AdaptiveTextSize.ExtraLarge,
                Weight = AdaptiveTextWeight.Bolder
            });

            card.Body.Add(new AdaptiveTextBlock
            {
                Text = "I can help you manage your tasks, leave comments, and generate reports. But we can keep chatting!",
                Wrap = true
            });

            card.Body.Add(new AdaptiveImage
            {
                Url = new Uri("https://cdn-icons-png.flaticon.com/512/3673/3673412.png"),
                Size = AdaptiveImageSize.Medium,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center
            });

            // Dodanie nowego bloku tekstu z pytaniem
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = "Do you want to start a task?",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                Spacing = AdaptiveSpacing.Large // Dodaje odstęp od poprzedniego elementu
            });
            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = "Show me my tasks",
                Data = new
                {
                    action = "showTasks"
                }
            });

            /*            // Dodanie przycisku (akcji)
                        card.Actions.Add(new AdaptiveSubmitAction
                        {
                            Title = "Show me my tasks",
                            // To jest kluczowa część - to tutaj przekazujesz dane
                            // i informujesz, gdzie je wysłać.
                            Data = new
                            {
                                msteams = new
                                {
                                    type = "adaptiveCard/action",
                                    value = new
                                    {
                                        action = "showTasks"
                                    }
                                }
                            }
                        });*/

            return card;
        }

        /// <summary>
        /// Creates a card displaying a list of tasks with an associated action button.
        /// Each task is displayed as a FactSet item.
        /// </summary>
        /// <param name="tasks">A collection of task names.</param>
        /// <param name="actionType">The type of action to perform on each task (e.g., 'comment', 'complete').</param>
        public AdaptiveCard CreateTaskListCard(IEnumerable<string> tasks, string actionType)
        {
            var card = new AdaptiveCard("1.2");
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = $"Available tasks:",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });

            foreach (var task in tasks)
            {
                var factSet = new AdaptiveFactSet();
                factSet.Facts.Add(new AdaptiveFact { Title = "Task", Value = task });
                card.Body.Add(factSet);

                var action = new AdaptiveActionSet();
                action.Actions.Add(new AdaptiveSubmitAction
                {
                    Title = actionType == "comment" ? "Leave a Comment" : "Mark as Done",
                    Data = new
                    {
                        action = actionType,
                        taskName = task
                    }
                });
                card.Body.Add(action);
            }

            return card;
        }

        /// <summary>
        /// Creates a card with an input field for a user to leave a comment on a specific task.
        /// It includes a text input and a submit button.
        /// </summary>
        /// <param name="taskName">The name of the task the comment is for.</param>
        public AdaptiveCard CreateCommentInputCard(int taskId, string taskName, string FromUserId)
        {
            var card = new AdaptiveCard("1.2");

            // Nagłówek
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = $"Comment on task: **{taskName}**",
                Size = AdaptiveTextSize.Large,
                Wrap = true
            });

            // Pole do wpisania komentarza
            card.Body.Add(new AdaptiveTextInput
            {
                Id = "commentInput",
                Placeholder = "Type your comment here...",
                IsMultiline = true
            });

            // Ukryty input z task ID
            card.Body.Add(new AdaptiveTextInput
            {
                Id = $"task_{taskId}",
                Value = taskId.ToString(),
                IsVisible = false
            });

            // Przycisk submit
            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = "Submit Comment",
                Data = new
                {
                    action = "submitComment",
                    fromUserId = FromUserId
                }
            });

            return card;
        }

        /// <summary>
        /// Creates a simple confirmation card with a message.
        /// The card contains a TextBlock element with the provided message.
        /// </summary>
        /// <param name="message">The confirmation message to display.</param>
        public AdaptiveCard CreateConfirmationCard(string message)
        {
            var card = new AdaptiveCard("1.2");

            card.Body.Add(new AdaptiveTextBlock
            {
                Text = message,
                Size = AdaptiveTextSize.Medium,
                Wrap = true,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center
            });

            return card;
        }

        /// <summary>
        /// Creates a card with options for a user to generate different types of reports.
        /// It presents options as buttons (Action.Submit).
        /// </summary>
        public AdaptiveCard CreateReportOptionsCard()
        {
            var card = new AdaptiveCard("1.2");

            card.Body.Add(new AdaptiveTextBlock
            {
                Text = "What kind of report would you like to generate?",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });

            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = "Generate Daily Report",
                Data = new
                {
                    action = "generateReport",
                    reportType = "daily"
                }
            });

            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = "Generate Weekly Report",
                Data = new
                {
                    action = "generateReport",
                    reportType = "weekly"
                }
            });

            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = "Generate Full Report",
                Data = new
                {
                    action = "generateReport",
                    reportType = "full"
                }
            });

            return card;
        }


        public AdaptiveCard CreateMultiTaskCommentCard(IEnumerable<(int Id, string Title)> tasks, string FromUserId)
        {
            var card = new AdaptiveCard("1.2");

            card.Body.Add(new AdaptiveTextBlock
            {
                Text = "Select task(s) to comment on:",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });

            // Dodanie checkboxów dla zadań
            foreach (var task in tasks)
            {
                card.Body.Add(new AdaptiveChoiceSetInput
                {
                    Id = $"task_{task.Id}",
                    IsMultiSelect = true,
                    Style = AdaptiveChoiceInputStyle.Expanded,
                    Choices = new List<AdaptiveChoice>
                    {
                        new AdaptiveChoice { Title = task.Title, Value = task.Id.ToString() }
                    }
                });
            }

            // Pole do wpisania komentarza
            card.Body.Add(new AdaptiveTextInput
            {
                Id = "commentInput",
                Placeholder = "Type your comment here...",
                IsMultiline = true
            });

            // Przycisk submit
            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = "Submit Comment",
                Data = new
                {
                    action = "submitComment",
                    fromUserId = FromUserId
                }
            });

            return card;
        }
        public AdaptiveCard CreateMultiTasksSelectionCard(IEnumerable<(int Id, string Title)> tasks, string FromUserId, string? Content = null, string? ActionType = null, string prompt = "Write your comment")
        {
            var taskList = tasks.ToList();
            var card = new AdaptiveCard("1.2");

            // Nagłówek
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = prompt,
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });
            if (ActionType == "CommentTaskByName")
            {
                // Pole do wpisania komentarza
                card.Body.Add(new AdaptiveTextInput
                {
                    Id = "commentInput",
                    Placeholder = "Wpisz komentarz...",
                    IsMultiline = true
                });
            }
            // Lista checkboxów tylko jeśli jest więcej niż jedno zadanie
            if (taskList.Count > 1)
            {
                foreach (var task in taskList)
                {
                    card.Body.Add(new AdaptiveChoiceSetInput
                    {
                        Id = $"task_{task.Id}",
                        IsMultiSelect = true,
                        Style = AdaptiveChoiceInputStyle.Expanded,
                        Choices = new List<AdaptiveChoice>
                {
                    new AdaptiveChoice { Title = task.Title, Value = task.Id.ToString() }
                }
                    });
                }
            }
            else if (taskList.Count == 1)
            {
                // Jeden task, możemy dodać ukryty input żeby wiedzieć do którego taska przypisać komentarz
                card.Body.Add(new AdaptiveTextInput
                {
                    Id = $"task_{taskList[0].Id}",
                    Value = taskList[0].Id.ToString(),
                    IsVisible = false
                });
            }

            // Przycisk Submit
            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = "Submit",
                Data = new
                {
                    action = "selectTasks",
                    fromUserId = FromUserId,
                    content = Content,
                    actionType = ActionType
                }
            });

            return card;
        }


        public AdaptiveCard CreateUpdateTaskCard(IEnumerable<(int Id, string Title)> tasks, string fromUserId, string prompt = "Select task(s) to update and provide new values:")
        {
            var card = new AdaptiveCard("1.2");

            // Nagłówek
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = prompt,
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });

            // Multi-select dla tasków
            card.Body.Add(new AdaptiveChoiceSetInput
            {
                Id = "selectedTasks",
                IsMultiSelect = true,
                Style = AdaptiveChoiceInputStyle.Expanded,
                Choices = tasks.Select(t => new AdaptiveChoice
                {
                    Title = t.Title,
                    Value = t.Id.ToString()
                }).ToList()
            });

            // Pole Title (opcjonalne)
            card.Body.Add(new AdaptiveTextInput
            {
                Id = "newTitle",
                Placeholder = "New title (optional)"
            });

            // Pole StartDate (opcjonalne)
            card.Body.Add(new AdaptiveDateInput
            {
                Id = "newStartDate",
                Placeholder = "Start date (optional)"
            });

            // Pole DueDate (opcjonalne)
            card.Body.Add(new AdaptiveDateInput
            {
                Id = "newDueDate",
                Placeholder = "Due date (optional)"
            });

            // Pole PercentComplete (opcjonalne)
            card.Body.Add(new AdaptiveNumberInput
            {
                Id = "newPercentComplete",
                Placeholder = "Percent complete (0–100)",
                Min = 0,
                Max = 100
            });

            // Submit
            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = "Update Task(s)",
                Data = new
                {
                    action = "updateTasks",
                    fromUserId = fromUserId
                }
            });

            return card;
        }

        public AdaptiveCard CreateTaskSelectionCard(
    List<(int Id, string Title)> tasks,
    bool onlyUserTasks,
    string fromUserId,
    string actionType)
        {
            var card = new AdaptiveCard("1.2");

            card.Body.Add(new AdaptiveTextBlock
            {
                Text = actionType == "startTask"
                    ? (onlyUserTasks
                        ? "Here are your active tasks assigned to you. Select one to start:"
                        : "Here are all active tasks. Select one to start:")
                    : (onlyUserTasks
                        ? "Here are your active tasks. Select one to stop:"
                        : "Here are all active tasks. Select one to stop:"),
                Weight = AdaptiveTextWeight.Bolder,
                Size = AdaptiveTextSize.Medium
            });

            foreach (var task in tasks)
            {
                card.Actions.Add(new AdaptiveSubmitAction
                {
                    Title = task.Title,
                    Data = new
                    {
                        action = "startStop",
                        actionType = actionType,  // "startTask" lub "stopTask"
                        taskId = task.Id,
                        taskName = task.Title,
                        fromUserId = fromUserId
                    }
                });
            }

            return card;
        }
        public AdaptiveCard CreateAddTimeToTaskCard(
        List<(int Id, string Title)> tasks,
        string fromUserId,
        bool needTime,
        bool needStart,
        bool needEnd,
        bool needPercent,
        string prompt)
        {
            var card = new AdaptiveCard("1.2");

            // Nagłówek karty
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = prompt,
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder,
                Wrap = true
            });

            // Informacja o alternatywach
            if (needTime || (needStart && needEnd))
            {
                card.Body.Add(new AdaptiveTextBlock
                {
                    Text = "ℹ️ You can either log total time OR provide start and end dates.",
                    Wrap = true,
                    Color = AdaptiveTextColor.Accent,
                    Size = AdaptiveTextSize.Small
                });
            }

            // Wybór taska (tylko jeśli jest więcej niż 1)
            if (tasks.Count > 1)
            {
                var choices = tasks
                    .Select(t => new AdaptiveChoice { Title = t.Title, Value = t.Id.ToString() })
                    .ToList();

                card.Body.Add(new AdaptiveChoiceSetInput
                {
                    Id = "selectedTask",
                    Style = AdaptiveChoiceInputStyle.Compact,
                    IsMultiSelect = false,
                    Choices = choices,
                    IsRequired = true
                });
            }

            // Dodawanie brakujących pól
            if (needTime)
            {
                card.Body.Add(new AdaptiveTextInput
                {
                    Id = "timeInput",
                    Placeholder = "Enter time in hours (e.g., 2.5)",
                    Style = AdaptiveTextInputStyle.Text,
                    IsRequired = true
                });
            }

            if (needStart)
            {
                card.Body.Add(new AdaptiveDateInput
                {
                    Id = "startDateInput",
                    Placeholder = "Select start date",
                    IsRequired = true
                });
                card.Body.Add(new AdaptiveTimeInput
                {
                    Id = "startTimeInput",
                    Placeholder = "Select start time",
                    IsRequired = true
                });
            }

            if (needEnd)
            {
                card.Body.Add(new AdaptiveDateInput
                {
                    Id = "endDateInput",
                    Placeholder = "Select end date",
                    IsRequired = true
                });
                card.Body.Add(new AdaptiveTimeInput
                {
                    Id = "endTimeInput",
                    Placeholder = "Select end time",
                    IsRequired = true
                });
            }

            if (needPercent)
            {
                card.Body.Add(new AdaptiveNumberInput
                {
                    Id = "percentInput",
                    Placeholder = "Enter percent complete",
                    Min = 0,
                    Max = 100,
                    IsRequired = true
                });
            }

            // Submit
            if (tasks.Count > 1)
            {
                card.Actions.Add(new AdaptiveSubmitAction
                {
                    Title = "Submit",
                    Data = new
                    {
                        action = "submitAddTime",
                        fromUserId = fromUserId
                    }
                });
            }
            else
            {
                // Jeśli tylko jeden task → automatycznie go przypisujemy
                card.Actions.Add(new AdaptiveSubmitAction
                {
                    Title = "Submit",
                    Data = new
                    {
                        action = "submitAddTime",
                        fromUserId = fromUserId,
                        selectedTask = tasks[0].Id
                    }
                });
            }

            return card;
        }





    }
}





