using AdaptiveCards;
using Application.Services;
using Domain.Entities;
using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Entities.Models;
using Domain.Interfaces;
using Infrastructure.Services.ActionHandlers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.MarkedNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Infrastructure.Services
{

    public class MyBot : ActivityHandler
    {
        private readonly IMessageParser _parser;
        private readonly IAdaptiveCardService _adaptiveCardService;
        private readonly ITaskFacade _taskFacade;
        private readonly ITaskRepository _taskRepository;
        private readonly IUserGraphService _userGraphService;
        private readonly ITimeEntryService _timeEntryService;


        public MyBot(IMessageParser parser, IAdaptiveCardService adaptiveCardService, ITaskFacade taskFacade, IUserGraphService userGraphService, ITaskRepository taskRepository, ITimeEntryService timeEntryService)
        {
            _parser = parser;
            _adaptiveCardService = adaptiveCardService;
            _taskFacade = taskFacade;
            _userGraphService = userGraphService;
            _taskRepository = taskRepository;
            _timeEntryService = timeEntryService;
        }

        protected override async Task OnMessageActivityAsync(
            ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Value != null)
            {
                var value = turnContext.Activity.Value as JObject;
                var action = value?["action"]?.ToString();

                if (!string.IsNullOrEmpty(action))
                {
                    switch (action)
                    {
                        case "showTasks":
                            await HandleShowTasksAsync(turnContext, cancellationToken);
                            return;

                        case "selectTasks":
                            await HandleMultipleTaskSelectionAsync(turnContext,value, cancellationToken);
                            return;
                        case "submitComment":
                            await HandleSubmitCommentCardAsync(turnContext, cancellationToken);
                            return;
                        case "updateTasks":
                            await HandleUpdateTaskCardAsync(turnContext, value, cancellationToken);
                            return;
                        case "startStop":
                            await HandleTaskStartStopSelectionCardAsync(turnContext, value, cancellationToken);
                            return;
                        case "submitAddTime":
                            await HandleSubmitAddTimeAsync(turnContext, value, cancellationToken);
                            return;
                        case "submitAddTask":
                            await HandleAddTaskSubmitAsync(turnContext, value, cancellationToken);
                            return;
                        default:
                            await turnContext.SendActivityAsync(
                                MessageFactory.Text("⚠️ I dont't understand this action."),
                                cancellationToken
                            );
                            return;
                    }
                }
            }

        

        // Teraz obsługujemy zwykłe wiadomości tekstowe
        var userMessage = turnContext.Activity.Text?.Trim();

            if (string.IsNullOrEmpty(userMessage))
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("I couldn,t understand an empty message."),
                    cancellationToken
                );
                return;
            }

            // Użycie parsera: messageThreadId -> losowy string, currentDate -> teraz
            ParsedMessageDto parsed = await _parser.Parse(
                messageThreadId: Guid.NewGuid().ToString(),
                message: userMessage,
                currentDate: DateTimeOffset.Now
            );
            var fromId = turnContext.Activity.From.Id; // 29:xxxxxxx...
            TeamsChannelAccount member;


            try
            {
                member = await TeamsInfo.GetMemberAsync(turnContext, fromId, cancellationToken);
            }
            catch (ErrorResponseException e) when (e.Body.Error.Code == "MemberNotFoundInConversation")
            {
                await turnContext.SendActivityAsync("I couldn't find user in this chat/channel.", cancellationToken: cancellationToken);
                return;
            }

            var FromUserId = member.AadObjectId;

            switch (parsed.Action)
            {
                case ActionType.Smalltalk:
                    {
                        string replyText = parsed.Reply ?? "Hey!";

                        // Tworzymy kartę powitalną
                        var welcomeCard = _adaptiveCardService.CreateWelcomeCard();

                        var attachment = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = welcomeCard
                        };

                        var reply = MessageFactory.Attachment(attachment);
                        reply.Text = replyText; // opcjonalny tekst nad kartą

                        await turnContext.SendActivityAsync(reply, cancellationToken);
                        break;
                    }
                case ActionType.AddTask:
                    {
                        if (string.IsNullOrWhiteSpace(parsed.TaskName))
                        {
                            // Pobieramy listę userów z teamu (np. z _userService albo z bazy)
                            var availableUsers = await _userGraphService.GetAllAsync();

                            var card = new AdaptiveCard("1.2");

                            card.Body.Add(new AdaptiveTextBlock
                            {
                                Text = "➕ Create a new task",
                                Weight = AdaptiveTextWeight.Bolder,
                                Size = AdaptiveTextSize.Medium
                            });

                            // Nazwa zadania (wymagana)
                            card.Body.Add(new AdaptiveTextInput
                            {
                                Id = "taskName",
                                Placeholder = "Enter task name",
                                IsRequired = true
                            });

                            // Data rozpoczęcia (opcjonalna)
                            card.Body.Add(new AdaptiveDateInput
                            {
                                Id = "startDate",
                                Placeholder = "Select start date"
                            });

                            // Due date (opcjonalna)
                            card.Body.Add(new AdaptiveDateInput
                            {
                                Id = "dueDate",
                                Placeholder = "Select due date"
                            });

                            // Lista użytkowników (checkboxy)
                            card.Body.Add(new AdaptiveTextBlock
                            {
                                Text = "Assign users:",
                                Weight = AdaptiveTextWeight.Bolder
                            });

                            var choices = availableUsers
                                .Select(u => new AdaptiveChoice { Title = u.DisplayName, Value = u.UserId })
                                .ToList();

                            card.Body.Add(new AdaptiveChoiceSetInput
                            {
                                Id = "assignedUsers",
                                IsMultiSelect = true,
                                Style = AdaptiveChoiceInputStyle.Expanded,
                                Choices = choices
                            });

                            // Submit
                            card.Actions.Add(new AdaptiveSubmitAction
                            {
                                Title = "Create Task",
                                Data = new
                                {
                                    action = "submitAddTask",
                                    fromUserId = FromUserId
                                }
                            });

                            var attachment = new Attachment
                            {
                                ContentType = AdaptiveCard.ContentType,
                                Content = card
                            };

                            await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
                            break;
                        }

                        // Jeśli user podał nazwę, tworzymy zadanie od razu
                        var targetUserIds = parsed.userIds ?? new List<string> { FromUserId };
                        targetUserIds = targetUserIds.Select(id => id == "-10" ? FromUserId : id).ToList();

                        // Walidacja dat
                        if (parsed.StartDate.HasValue && parsed.EndDate.HasValue &&
                            parsed.StartDate > parsed.EndDate)
                        {
                            await turnContext.SendActivityAsync("⚠️ Start date cannot be later than due date.", cancellationToken: cancellationToken);
                            break;
                        }

                        var newTask = await _taskFacade.CreateTaskAsync(parsed.TaskName);
                        await _taskFacade.UpdateTaskAsync(newTask, new UpdateTaskDto
                        {
                            UsersToAssign = targetUserIds,
                            StartDate = parsed.StartDate,
                            DueDate = parsed.EndDate
                        });

                        await turnContext.SendActivityAsync(
                            MessageFactory.Text($"✅ Task '{newTask.Title}' was added."),
                            cancellationToken
                        );
                        break;
                    }

                case ActionType.CommentTaskByName:
                    {
                        var taskName = parsed.TaskName;

                        // Pobieramy wszystkie zadania użytkownika
                        var allUserTasks = await _taskRepository.GetUserTasksAsync(FromUserId);
                        var userTasks = allUserTasks.Select(t => (t.Id, t.Title)).ToList();

                        List<(int Id, string Title)> tasksToShow;

                        if (!string.IsNullOrWhiteSpace(taskName))
                        {
                            // Filtrowanie po nazwie
                            tasksToShow = userTasks
                                .Where(t => t.Title.Equals(taskName, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            if (!tasksToShow.Any())
                            {
                                await turnContext.SendActivityAsync(
                                    MessageFactory.Text($"⚠️ You have no tasks named '{taskName}'."),
                                    cancellationToken
                                );
                                break;
                            }
                        }
                        else
                        {
                            // Brak TaskName → pokazujemy wszystkie
                            tasksToShow = userTasks;
                            if (!tasksToShow.Any())
                            {
                                await turnContext.SendActivityAsync(
                                    MessageFactory.Text("⚠️ You have no tasks to comment."),
                                    cancellationToken
                                );
                                break;
                            }
                        }

                        // Jeśli od razu mamy treść komentarza
                        if (!string.IsNullOrWhiteSpace(parsed.Content))
                        {
                            if (tasksToShow.Count == 1)
                            {
                                await _taskFacade.AddCommentAsync(FromUserId,
                                    await _taskRepository.GetTaskById(tasksToShow[0].Id),
                                    parsed.Content);

                                await turnContext.SendActivityAsync(
                                    MessageFactory.Text($"💬 Comment added to task '{tasksToShow[0].Title}'."),
                                    cancellationToken
                                );
                            }
                            else
                            {
                                // Wiele zadań -> multiwybór
                                var multiTasksCard = _adaptiveCardService.CreateMultiTasksSelectionCard(
                                    tasksToShow,
                                    FromUserId,
                                    parsed.Content,
                                    parsed.Action.ToString(),
                                    taskName != null
                                        ? $"Multiple tasks found named '{taskName}'. Select where to add your comment:"
                                        : "Multiple tasks found. Select where to add your comment:"
                                );

                                var attachment = new Attachment
                                {
                                    ContentType = AdaptiveCard.ContentType,
                                    Content = multiTasksCard
                                };

                                await turnContext.SendActivityAsync(
                                    MessageFactory.Attachment(attachment),
                                    cancellationToken
                                );
                            }
                        }
                        else
                        {
                            // Content pusty -> pokaż kartę z polem komentarza
                            AdaptiveCard commentCard;

                            if (tasksToShow.Count == 1)
                            {
                                commentCard = _adaptiveCardService.CreateCommentInputCard(tasksToShow[0].Id,tasksToShow[0].Title, FromUserId);
                            }
                            else
                            {
                                commentCard = _adaptiveCardService.CreateMultiTaskCommentCard(
                                    tasksToShow,
                                    FromUserId
                                );
                            }

                            var attachment = new Attachment
                            {
                                ContentType = AdaptiveCard.ContentType,
                                Content = commentCard
                            };

                            await turnContext.SendActivityAsync(
                                MessageFactory.Attachment(attachment),
                                cancellationToken
                            );
                        }

                        break;
                    }


                case ActionType.DeleteTaskByName:
                    {
                        var taskName = parsed.TaskName;

                        // Pobierz wszystkie zadania użytkownika
                        var allUserTasks = await _taskRepository.GetUserTasksAsync(FromUserId);
                        var userTasks = allUserTasks.Select(t => (t.Id, t.Title)).ToList();

                        List<(int Id, string Title)> tasksToShow;

                        if (!string.IsNullOrWhiteSpace(taskName))
                        {
                            // Filtrowanie po nazwie
                            tasksToShow = userTasks
                                .Where(t => t.Title.Equals(taskName, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            if (!tasksToShow.Any())
                            {
                                await turnContext.SendActivityAsync(
                                    $"⚠️ You have no tasks named '{taskName}'.",
                                    cancellationToken: cancellationToken
                                );
                                break;
                            }
                        }
                        else
                        {
                            // Brak TaskName → pokazujemy wszystkie zadania
                            tasksToShow = userTasks;
                            if (!tasksToShow.Any())
                            {
                                await turnContext.SendActivityAsync(
                                    "⚠️ You have no tasks to delete.",
                                    cancellationToken: cancellationToken
                                );
                                break;
                            }
                        }

                        if (tasksToShow.Count == 1)
                        {
                            var taskToDelete = await _taskRepository.GetTaskById(tasksToShow[0].Id);
                            if (taskToDelete != null)
                            {
                                await _taskFacade.DeleteTaskAsync(taskToDelete);
                                await turnContext.SendActivityAsync(
                                    $"🗑️ Task '{tasksToShow[0].Title}' was deleted.",
                                    cancellationToken: cancellationToken
                                );
                            }
                        }
                        else
                        {
                            // Wiele zadań → wyświetlamy AdaptiveCard do wyboru
                            var card = _adaptiveCardService.CreateMultiTasksSelectionCard(
                                tasks: tasksToShow,
                                FromUserId: FromUserId,
                                Content: null,
                                ActionType: parsed.Action.ToString(),
                                prompt: taskName != null
                                    ? $"Multiple tasks found named '{taskName}'. Select which ones to delete:"
                                    : "Select which task(s) to delete:"
                            );

                            var attachment = new Attachment
                            {
                                ContentType = AdaptiveCard.ContentType,
                                Content = card
                            };

                            await turnContext.SendActivityAsync(
                                MessageFactory.Attachment(attachment),
                                cancellationToken
                            );
                        }

                        break;
                    }


                case ActionType.UpdateTaskByName:
                    {
                        var tasks = await _taskFacade.GetTasksByNameAsync(parsed.TaskName);
                        if (!tasks.Any())
                        {
                            await turnContext.SendActivityAsync(
                                MessageFactory.Text($"⚠️ No task found with given name: '{parsed.TaskName}'."),
                                cancellationToken
                            );
                            break;
                        }

                        // sprawdzamy czy user podał dane do update
                        bool hasUpdateData = !string.IsNullOrWhiteSpace(parsed.NewName)
                            || parsed.StartDate.HasValue
                            || parsed.EndDate.HasValue
                            || parsed.PercentComplete.HasValue
                            || (parsed.userIds != null && parsed.userIds.Any());

                        if (hasUpdateData)
                        {
                            // ✅ mamy dane → update wszystkich tasków o tej nazwie
                            var updateDto = new UpdateTaskDto
                            {
                                Title = parsed.NewName,
                                StartDate = parsed.StartDate,
                                DueDate = parsed.EndDate,
                                PercentComplete = parsed.PercentComplete,
                                UsersToAssign = parsed.userIds ?? new List<string> { FromUserId }
                            };

                            foreach (var t in tasks)
                            {
                                await _taskFacade.UpdateTaskByIdAsync(t.Id, updateDto);
                            }

                            await turnContext.SendActivityAsync(
                                MessageFactory.Text($"✏️ {tasks.Count} task(s) named '{parsed.TaskName}' were updated."),
                                cancellationToken
                            );
                        }
                        else
                        {
                            // ❌ brak danych → pokazujemy adaptive card do wyboru tasków i podania parametrów
                            var card = _adaptiveCardService.CreateUpdateTaskCard(
                                tasks.Select(t => (t.Id, t.Title)),
                                FromUserId,
                                $"🔎 Found {tasks.Count} tasks named '{parsed.TaskName}'. Select which to update and provide new values:"
                            );

                            var attachment = new Attachment
                            {
                                ContentType = AdaptiveCard.ContentType,
                                Content = card
                            };

                            await turnContext.SendActivityAsync(
                                MessageFactory.Attachment(attachment),
                                cancellationToken
                            );
                        }

                        break;
                    }
                case ActionType.StartTaskByName:
                    {
                        var messageDto = new TeamsMessageDto
                        {
                            FromUserId = FromUserId,
                            ReplyToId = turnContext.Activity.Id
                        };

                        if (string.IsNullOrWhiteSpace(parsed.TaskName))
                        {
                            // user nic nie podał → pobieramy wszystkie zadania przypisane do usera
                            var userTasks = await _taskRepository.GetUserTasksAsync(FromUserId);
                            if (!userTasks.Any())
                            {
                                await turnContext.SendActivityAsync("⚠️ You don’t have any active tasks assigned.", cancellationToken: cancellationToken);
                                break;
                            }

                            var card = _adaptiveCardService.CreateTaskSelectionCard(
                                userTasks.Select(t => (t.Id, t.Title)).ToList(),
                                onlyUserTasks: true,
                                FromUserId,
                                "startTask"
                            );

                            var attachment = new Attachment
                            {
                                ContentType = AdaptiveCard.ContentType,
                                Content = card
                            };

                            await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
                        }
                        else
                        {
                            // user podał nazwę
                            var tasks = await _taskFacade.GetTasksByNameAsync(parsed.TaskName);

                            if (tasks.Count == 0)
                            {
                                await turnContext.SendActivityAsync(
                                    $"⚠️ No task found with the name '{parsed.TaskName}'.",
                                    cancellationToken: cancellationToken
                                );
                            }
                            else if (tasks.Count == 1)
                            {
                                // jeśli zadanie przypisane do usera, to startujemy
                                var task = tasks.First();
                                var isassigned = await _taskFacade.IsUserAssignedAsync(task.Id, FromUserId);
                                if (!isassigned)
                                {
                                    await turnContext.SendActivityAsync($"⚠️ No assigned to you tasks found with the name {parsed.TaskName}.", cancellationToken: cancellationToken);
                                    break;
                                }

                                try
                                {
                                    await _timeEntryService.StartTaskAsync(FromUserId, task);
                                    await turnContext.SendActivityAsync($"▶️ Started task '{task.Title}'.", cancellationToken: cancellationToken);
                                }
                                catch (InvalidOperationException ex)
                                {
                                    await turnContext.SendActivityAsync($"⚠️ Cannot start task: {ex.Message}", cancellationToken: cancellationToken);
                                }
                            }
                            else
                            {
                                // kilka zadań → pokaż AdaptiveCard do wyboru tylko jednego
                                var card = _adaptiveCardService.CreateTaskSelectionCard(
                                    tasks.Select(t => (t.Id, t.Title)).ToList(),
                                    onlyUserTasks: true,
                                    FromUserId,
                                    "startTask"
                                );

                                var attachment = new Attachment
                                {
                                    ContentType = AdaptiveCard.ContentType,
                                    Content = card
                                };

                                await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
                            }
                        }

                        break;
                    }
                case ActionType.StopTaskByName:
                    {
                        var taskEntries = new List<(TaskModel Task, TimeEntryModel TimeEntry)>();

                        if (string.IsNullOrWhiteSpace(parsed.TaskName))
                        {
                            // wszystkie zadania przypisane do usera i aktywne
                            var activeTasks = await _timeEntryService.GetActiveTasksAsync(FromUserId);
                            foreach (var task in activeTasks)
                            {
                                var timeEntries = await _timeEntryService.GetActiveTimeEntriesAsync(FromUserId, task.Id);
                                foreach (var entry in timeEntries)
                                    taskEntries.Add((task, entry));
                            }
                        }
                        else
                        {
                            // zadania o podanej nazwie i aktywne
                            var foundTasks = await _taskFacade.GetTasksByNameAsync(parsed.TaskName);
                            if (foundTasks != null)
                            {
                                foreach (var task in foundTasks)
                                {
                                    var timeEntries = await _timeEntryService.GetActiveTimeEntriesAsync(FromUserId, task.Id);
                                    foreach (var entry in timeEntries)
                                        taskEntries.Add((task, entry));
                                }
                            }
                        }

                        if (taskEntries.Count == 0)
                        {
                            await turnContext.SendActivityAsync(
                                MessageFactory.Text("⚠️ No active tasks found to stop."),
                                cancellationToken
                            );
                            break;
                        }

                        if (taskEntries.Count == 1)
                        {
                            var (task, entry) = taskEntries[0];
                            int? percent = parsed.PercentComplete;

                            if (!percent.HasValue && task.PercentComplete == 0)
                                percent = 50;

                            await _timeEntryService.StopTaskAsync(FromUserId, task, percent);

                            string extra = percent.HasValue ? $" {percent}%" : string.Empty;

                            await turnContext.SendActivityAsync(
                                MessageFactory.Text($"⏹️ Stopped task '{task.Title}'{extra}."),
                                cancellationToken
                            );
                            break;
                        }

                        // kilka zadań -> AdaptiveCard
                        var card = _adaptiveCardService.CreateTaskSelectionCard(
                            taskEntries.Select(te => (te.Task.Id, $"{te.Task.Title} (entry {te.TimeEntry.Id})")).ToList(),
                            onlyUserTasks: true,
                            actionType: "stopTask",
                            fromUserId: FromUserId
                        );


                        var attachment = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = card
                        };

                        await turnContext.SendActivityAsync(
                            MessageFactory.Attachment(attachment),
                            cancellationToken
                        );
                        break;
                    }

                case ActionType.AddTimeToTaskByName:
                    {
                        // Pobierz wszystkie taski przypisane do użytkownika
                        var allUserTasks = await _taskRepository.GetUserTasksAsync(FromUserId);
                        var userTasks = allUserTasks.Select(t => (t.Id, t.Title)).ToList();

                        var taskName = parsed.TaskName;
                        var timeProvided = parsed.Time.HasValue;
                        var startProvided = parsed.StartDate.HasValue;
                        var endProvided = parsed.EndDate.HasValue;
                        var percentProvided = parsed.PercentComplete.HasValue;

                        List<(int Id, string Title)> tasksToShow;

                        if (!string.IsNullOrWhiteSpace(taskName))
                        {
                            tasksToShow = userTasks
                                .Where(t => t.Title.Equals(taskName, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            if (!tasksToShow.Any())
                            {
                                await turnContext.SendActivityAsync($"⚠️ You have no tasks named '{taskName}'.", cancellationToken: cancellationToken);
                                break;
                            }
                        }
                        else
                        {
                            tasksToShow = userTasks;
                            if (!tasksToShow.Any())
                            {
                                await turnContext.SendActivityAsync("⚠️ You have no active tasks to add time to.", cancellationToken: cancellationToken);
                                break;
                            }
                        }

                        // Jeśli mamy wiele tasków → zawsze pokazujemy kartę do wyboru taska
                        if (tasksToShow.Count > 1)
                        {
                            // Wyznacz brakujące pola
                            bool needTime = false;
                            bool needStart = !startProvided && !(timeProvided || endProvided);
                            bool needEnd = !endProvided && !(timeProvided || startProvided);
                            bool needPercent = !percentProvided;

                            var card = _adaptiveCardService.CreateAddTimeToTaskCard(
                                tasks: tasksToShow,
                                fromUserId: FromUserId,
                                needTime: needTime,
                                needStart: needStart,
                                needEnd: needEnd,
                                needPercent: needPercent,
                                prompt: taskName != null
                                    ? $"Specify which '{taskName}' and fill missing info"
                                    : "Select a task and provide missing info"
                            );

                            var attachment = new Attachment
                            {
                                ContentType = AdaptiveCard.ContentType,
                                Content = card
                            };

                            await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
                            return;
                        }

                        // Dokładnie jeden task
                        var task = tasksToShow.First();

                        // Jeśli mamy już wszystkie niezbędne informacje (time lub start+end) → wykonaj akcję od razu
                        if (timeProvided || (startProvided && endProvided))
                        {
                            await _timeEntryService.AddTimeToTaskById(
                                FromUserId,
                                task.Id,
                                parsed.Time,
                                parsed.PercentComplete,
                                parsed.StartDate,
                                parsed.EndDate
                            );

                            await turnContext.SendActivityAsync(
                                $"Logged {parsed.Time?.ToString() ?? $"{parsed.StartDate}-{parsed.EndDate}"} to task {task.Title}.",
                                cancellationToken: cancellationToken
                            );
                            return;
                        }

                        // Wyznacz brakujące pola, ale tylko te, które nie są już podane
                        bool missingTime = false;
                        bool missingStart = !startProvided && !(timeProvided || endProvided);
                        bool missingEnd = !endProvided && !(timeProvided || startProvided);
                        bool missingPercent = !percentProvided;

                        // nothing is missing
                        if (!missingTime || ( !missingStart && !missingEnd))
                        {
                            await _timeEntryService.AddTimeToTaskById(
                                FromUserId,
                                task.Id,
                                parsed.Time,
                                parsed.PercentComplete,
                                parsed.StartDate,
                                parsed.EndDate
                            );

                            await turnContext.SendActivityAsync(
                                $"Logged {parsed.Time?.ToString() ?? $"{parsed.StartDate}-{parsed.EndDate}"} to task {task.Title}.",
                                cancellationToken: cancellationToken
                            );
                            return;
                        }
                        //only missing data
                        var singleCard = _adaptiveCardService.CreateAddTimeToTaskCard(
                            tasks: new List<(int Id, string Title)> { task },
                            fromUserId: FromUserId,
                            needTime: missingTime,
                            needStart: missingStart,
                            needEnd: missingEnd,
                            needPercent: missingPercent,
                            prompt: "Fill in missing info for this task"
                        );

                        var singleAttachment = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = singleCard
                        };

                        await turnContext.SendActivityAsync(MessageFactory.Attachment(singleAttachment), cancellationToken);
                        break;
                    }

                default:
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("⚠️ I don't understand this command, or it is not yet implemented."),
                        cancellationToken
                    );
                    break;
            }
        }



        private async Task HandleShowTasksAsync(
    ITurnContext turnContext,
    CancellationToken cancellationToken,
    bool onlyUserTasks = false)
        {
            var fromId = turnContext.Activity.From.Id; // 29:xxxxxxx...
            TeamsChannelAccount member;


            try
            {
                member = await TeamsInfo.GetMemberAsync(turnContext, fromId, cancellationToken);
            }
            catch (ErrorResponseException e) when (e.Body.Error.Code == "MemberNotFoundInConversation")
            {
                await turnContext.SendActivityAsync("Couldn't find user in this chat/channel.", cancellationToken: cancellationToken);
                return;
            }
            var fromUserId = member.AadObjectId;

            // Pobranie listy zadań
            List<(int Id, string Title)> tasks;
            
            if (onlyUserTasks)
            {
                // Pobranie tylko zadań przypisanych do użytkownika
                var userTasks = await _taskRepository.GetUserTasksAsync(fromUserId);

                // Mapujemy do (int Id, string Title)
                tasks = userTasks.Select(t => (t.Id, t.Title)).ToList();
            }
            else
            {
                // Pobranie wszystkich zadań
                var allTasks = await _taskRepository.GetAllAsync();
                // Fix for CS0266: Explicitly convert the IEnumerable to a List using .ToList()
                tasks = allTasks.Select(t => (t.Id, t.Title)).ToList();
            }

            if (tasks == null || tasks.Count == 0)
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("⚠️ No tasks found."),
                    cancellationToken
                );
                return;
            }

            var card = _adaptiveCardService.CreateTaskSelectionCard(tasks, onlyUserTasks, fromUserId, "startTask");

            var attachment = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };

            await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
        }


        private async Task HandleMultipleTaskSelectionAsync(
     ITurnContext turnContext,
     JObject value,
     CancellationToken cancellationToken)
        {
            // Typ akcji: "submitComment" lub "delete"
            var actionType = value?["actionType"]?.ToString();
            if (string.IsNullOrEmpty(actionType))
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("⚠️ No actionType provided."),
                    cancellationToken
                );
                return;
            }

            // Wyciągamy wszystkie pola zaczynające się od "task_"
            var selectedTaskIds = value.Properties()
                .Where(p => p.Name.StartsWith("task_"))
                .Select(p => int.TryParse(p.Value.ToString(), out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();

            if (!selectedTaskIds.Any())
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("❌ No tasks selected. Please select at least one task."),
                    cancellationToken
                );
                return;
            }

            var fromUserId = value?["fromUserId"]?.ToString();
            var content = value?["content"]?.ToString(); // dla komentarza

            switch (actionType)
            {
                case "CommentTaskByName":
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        await turnContext.SendActivityAsync(
                            MessageFactory.Text("❌ Comment is empty. Please provide a comment."),
                            cancellationToken
                        );
                        return;
                    }

                    foreach (var taskId in selectedTaskIds)
                    {
                        var task = await _taskRepository.GetTaskById(taskId);
                        if (task != null)
                        {
                            await _taskFacade.AddCommentAsync(fromUserId, task, content);
                        }
                    }

                    await turnContext.SendActivityAsync(
                        MessageFactory.Text($"💬 Comment added to {selectedTaskIds.Count} task(s)."),
                        cancellationToken
                    );
                    break;

                case "DeleteTaskByName":
                    foreach (var taskId in selectedTaskIds)
                    {
                        var task = await _taskRepository.GetTaskById(taskId);
                        if (task != null)
                        {
                            await _taskFacade.DeleteTaskAsync(task);
                        }
                    }

                    await turnContext.SendActivityAsync(
                        MessageFactory.Text($"🗑️ Deleted {selectedTaskIds.Count} task(s)."),
                        cancellationToken
                    );
                    break;

                default:
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("⚠️ Unknown actionType. Supported: submitComment, delete."),
                        cancellationToken
                    );
                    break;
            }
        }

        private async Task HandleSubmitCommentCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var value = turnContext.Activity.Value as JObject;
            if (value == null)
            {
                await turnContext.SendActivityAsync("❌ Couldn't read data from card.", cancellationToken: cancellationToken);
                return;
            }

            // user id
            var fromUserId = value["fromUserId"]?.ToString();

            // comment
            var comment = value["commentInput"]?.ToString();
            if (string.IsNullOrWhiteSpace(comment))
            {
                await turnContext.SendActivityAsync("❌ Comment is empty.", cancellationToken: cancellationToken);
                return;
            }

            // task_{TaskId}
            var selectedTaskIds = value.Properties()
     .Where(p => p.Name.StartsWith("task_"))
     .Select(p =>
     {
         var v = p.Value;
         return v.Type == JTokenType.Integer ? (int)v : int.Parse(v.ToString());
     })
     .ToList();
            if (selectedTaskIds.Count == 0)
            {
                await turnContext.SendActivityAsync("❌ No tasks chosen. Pick at least one.", cancellationToken: cancellationToken);
                return;
            }

            // add comment to each task
            foreach (var taskId in selectedTaskIds)
            {
                var task = await _taskRepository.GetTaskById(taskId);
                if (task != null)
                {
                    await _taskFacade.AddCommentAsync(fromUserId, task, comment);
                }
            }

            await turnContext.SendActivityAsync(
                $"💬 Comment added to {selectedTaskIds.Count} task/s.",
                cancellationToken: cancellationToken
            );
        }
        private async Task HandleUpdateTaskCardAsync(
    ITurnContext turnContext,
    JObject value,
    CancellationToken cancellationToken)
        {
            // 1. get tasks from checkboxes
            var selectedTaskIds = value["selectedTasks"]?.ToString()
                ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => int.TryParse(id, out var parsedId) ? parsedId : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList() ?? new List<int>();

            if (!selectedTaskIds.Any())
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("❌ No tasks selected. Please select at least one task."),
                    cancellationToken
                );
                return;
            }

            // 2. Get forms data
            var fromUserId = value["fromUserId"]?.ToString();
            var newTitle = value["newTitle"]?.ToString();
            var startDateStr = value["newStartDate"]?.ToString();
            var dueDateStr = value["newDueDate"]?.ToString();
            var percentStr = value["newPercentComplete"]?.ToString();

            DateTimeOffset? startDate = DateTimeOffset.TryParse(startDateStr, out var s) ? s : null;
            DateTimeOffset? dueDate = DateTimeOffset.TryParse(dueDateStr, out var d) ? d : null;
            int? percent = int.TryParse(percentStr, out var p) ? p : null;

            // 3. Validate dates
            if (startDate.HasValue && dueDate.HasValue && dueDate < startDate)
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("⚠️ The due date cannot be earlier than the start date."),
                    cancellationToken
                );
                return;
            }

            // 4. Build DTO
            var updateDto = new UpdateTaskDto
            {
                Title = newTitle,
                StartDate = startDate,
                DueDate = dueDate,
                PercentComplete = percent,
                UsersToAssign = new List<string> { fromUserId }
            };

            // 5. update all chosen tasks
            foreach (var taskId in selectedTaskIds)
            {
                var task = await _taskRepository.GetTaskById(taskId);
                if (task != null)
                {
                    await _taskFacade.UpdateTaskByIdAsync(task.Id, updateDto);
                }
            }

            // 6. Confirm
            await turnContext.SendActivityAsync(
                MessageFactory.Text($"✏️ {selectedTaskIds.Count} task(s) have been updated."),
                cancellationToken
            );
        }
        private async Task HandleTaskStartStopSelectionCardAsync(
     ITurnContext turnContext,
     JObject value,
     CancellationToken cancellationToken)
        {
            var fromUserId = value["fromUserId"]?.ToString();
            if (string.IsNullOrEmpty(fromUserId))
            {
                await turnContext.SendActivityAsync("❌ Missing user information.", cancellationToken: cancellationToken);
                return;
            }

            var selectedTaskIdStr = value["taskId"]?.ToString();
            if (string.IsNullOrEmpty(selectedTaskIdStr) || !int.TryParse(selectedTaskIdStr, out var selectedTaskId))
            {
                await turnContext.SendActivityAsync("❌ No task was selected.", cancellationToken: cancellationToken);
                return;
            }
            var actionType = value["actionType"]?.ToString();
            if (string.IsNullOrEmpty(actionType))
            {
                await turnContext.SendActivityAsync("⚠️ Unknown action type.", cancellationToken: cancellationToken);
                return;
            }

            var task = await _taskRepository.GetTaskById(selectedTaskId);
            if (task == null)
            {
                await turnContext.SendActivityAsync("⚠️ Task not found.", cancellationToken: cancellationToken);
                return;
            }

            var isUserAssigned = await _taskFacade.IsUserAssignedAsync(selectedTaskId, fromUserId);
            if (!isUserAssigned)
            {
                await turnContext.SendActivityAsync("⚠️ You can only manage tasks assigned to you.", cancellationToken: cancellationToken);
                return;
            }

            try
            {
                if (actionType == "startTask")
                {
                    await _timeEntryService.StartTaskAsync(fromUserId, task);
                    await turnContext.SendActivityAsync($"▶️ Started task '{task.Title}'.", cancellationToken: cancellationToken);
                }
                else if (actionType == "stopTask")
                {
                    
                    await _timeEntryService.StopTaskAsync(fromUserId, task, task.PercentComplete);
                    await turnContext.SendActivityAsync($"⏹️ Stopped task '{task.Title}'.", cancellationToken: cancellationToken);
                }
                else
                {
                    await turnContext.SendActivityAsync("⚠️ Unknown action type.", cancellationToken: cancellationToken);
                }
            }
            catch (InvalidOperationException ex)
            {
                await turnContext.SendActivityAsync($"⚠️ Cannot {actionType} task: {ex.Message}", cancellationToken: cancellationToken);
            }
        }
        private async Task HandleSubmitAddTimeAsync(
    ITurnContext turnContext,
    JObject value,
    CancellationToken cancellationToken)
        {
            var fromUserId = value["fromUserId"]?.ToString();
            if (string.IsNullOrEmpty(fromUserId))
            {
                await turnContext.SendActivityAsync("❌ Missing user information.", cancellationToken: cancellationToken);
                return;
            }

            var selectedTaskStr = value["selectedTask"]?.ToString();
            if (string.IsNullOrEmpty(selectedTaskStr) || !int.TryParse(selectedTaskStr, out var selectedTaskId))
            {
                await turnContext.SendActivityAsync("⚠️ No task was selected.", cancellationToken: cancellationToken);
                return;
            }

            // Parse
            int? percent = null;
            if (int.TryParse(value["percentInput"]?.ToString(), out var parsedPercent))
                percent = parsedPercent;

            // Parse
            double? time = null;
            if (double.TryParse(value["timeInput"]?.ToString(), out var parsedTime))
                time = parsedTime;

            // Parse start + end
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            var startDateStr = value["startDateInput"]?.ToString();
            var startTimeStr = value["startTimeInput"]?.ToString();
            if (!string.IsNullOrEmpty(startDateStr) && !string.IsNullOrEmpty(startTimeStr))
            {
                if (DateTime.TryParse($"{startDateStr}T{startTimeStr}", out var parsedStart))
                    startDateTime = parsedStart;
            }

            var endDateStr = value["endDateInput"]?.ToString();
            var endTimeStr = value["endTimeInput"]?.ToString();
            if (!string.IsNullOrEmpty(endDateStr) && !string.IsNullOrEmpty(endTimeStr))
            {
                if (DateTime.TryParse($"{endDateStr}T{endTimeStr}", out var parsedEnd))
                    endDateTime = parsedEnd;
            }

            // Walidacja: albo time, albo start+end
            if (!time.HasValue && (!startDateTime.HasValue || !endDateTime.HasValue))
            {
                await turnContext.SendActivityAsync("⚠️ Please provide either total time OR both start and end.", cancellationToken: cancellationToken);
                return;
            }

            try
            {
                await _timeEntryService.AddTimeToTaskById(
                    fromUserId,
                    selectedTaskId,
                    time,
                    percent,
                    startDateTime,
                    endDateTime);

                var msg = time.HasValue
                    ? $"⏱️ Logged {time}h to task."
                    : $"⏱️ Logged time from {startDateTime} to {endDateTime}.";

                if (percent.HasValue)
                    msg += $" Progress set to {percent}%.";

                await turnContext.SendActivityAsync(msg, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await turnContext.SendActivityAsync($"❌ Failed to log time: {ex.Message}", cancellationToken: cancellationToken);
            }
        }
        private async Task HandleAddTaskSubmitAsync(
    ITurnContext turnContext,
    JObject value,
    CancellationToken cancellationToken)
        {
            var fromUserId = value["fromUserId"]?.ToString();
            if (string.IsNullOrEmpty(fromUserId))
            {
                await turnContext.SendActivityAsync("❌ Missing user information.", cancellationToken: cancellationToken);
                return;
            }

            var taskName = value["taskName"]?.ToString();
            if (string.IsNullOrWhiteSpace(taskName))
            {
                await turnContext.SendActivityAsync("⚠️ Task name is required.", cancellationToken: cancellationToken);
                return;
            }

            // Parsowanie dates
            DateTime? startDate = null;
            DateTime? dueDate = null;

            if (DateTime.TryParse(value["startDate"]?.ToString(), out var parsedStart))
                startDate = parsedStart;

            if (DateTime.TryParse(value["dueDate"]?.ToString(), out var parsedDue))
                dueDate = parsedDue;

            if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            {
                await turnContext.SendActivityAsync("⚠️ Start date cannot be later than due date.", cancellationToken: cancellationToken);
                return;
            }

            // Parse assigned users
            var assignedUsersRaw = value["assignedUsers"]?.ToString();
            var assignedUsers = !string.IsNullOrEmpty(assignedUsersRaw)
                ? assignedUsersRaw.Split(',').ToList()
                : new List<string> { fromUserId };

            // Create task
            var newTask = await _taskFacade.CreateTaskAsync(taskName);
            await _taskFacade.UpdateTaskAsync(newTask, new UpdateTaskDto
            {
                UsersToAssign = assignedUsers,
                StartDate = startDate,
                DueDate = dueDate
            });

            await turnContext.SendActivityAsync(
                $"✅ Task '{newTask.Title}' created and assigned to {assignedUsers.Count} user(s).",
                cancellationToken: cancellationToken
            );
        }


    }

}
