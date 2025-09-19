using Common.Data;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _dbContext;
        private readonly IUserRepository _userRepository;

        public ReportService(AppDbContext dbContext, IUserRepository userRepository)
        {
            _dbContext = dbContext;
            _userRepository = userRepository;
        }

        public async Task<UserReport> GetUserReportAsync(string userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.");
            }
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                var temp = startDate;
                startDate = endDate;
                endDate = temp;
            }
            var query = _dbContext.TimeEntries.AsQueryable()
                .Where(te => te.UserId == userId);

            if (startDate.HasValue)
            {
                var utcStart = startDate.Value.UtcDateTime;
                query = query.Where(te => te.StartTime >= utcStart);
            }

            if (endDate.HasValue)
            {
                var utcEnd = endDate.Value.UtcDateTime;
                query = query.Where(te => te.EndTime <= utcEnd);
            }

            var allTimeEntries = await query
                .Select(te => new
                {
                    te.LocalTaskId,
                    te.UserId,
                    te.StartTime,
                    te.EndTime,
                    te.Duration
                })
                .ToListAsync();

            return new UserReport
            {
                UserId = userId,
                TotalHours = allTimeEntries.Sum(te => te.Duration.TotalHours),
                TotalEntries = allTimeEntries.Count,
                TotalTasks = allTimeEntries.Select(te => te.LocalTaskId).Distinct().Count()
            };
        }

        public async Task<OverallReport> GetOverallReportAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                var temp = startDate;
                startDate = endDate;
                endDate = temp;
            }
            var query = _dbContext.TimeEntries.AsQueryable();

            if (startDate.HasValue)
            {
                var utcStart = startDate.Value.UtcDateTime;
                query = query.Where(te => te.StartTime >= utcStart);
            }

            if (endDate.HasValue)
            {
                var utcEnd = endDate.Value.UtcDateTime;
                query = query.Where(te => te.EndTime <= utcEnd);
            }

            var allTimeEntries = await query
                    .Select(te => new
                    {
                        te.LocalTaskId,
                        te.UserId,
                        te.StartTime,
                        te.EndTime,
                        te.Duration
                    })
                    .ToListAsync();

            var report = new OverallReport
            {
                TotalEntries = allTimeEntries.Count,
                TotalHours = allTimeEntries.Sum(te => te.Duration.TotalHours),
                TotalTasks = allTimeEntries.Select(te => te.LocalTaskId).Distinct().Count(),
            };

            if (allTimeEntries.Count > 0)
            {
                var userDurations = allTimeEntries
                    .GroupBy(te => te.UserId)
                    .Select(g => new { UserId = g.Key, TotalDuration = g.Sum(te => te.Duration.TotalHours) })
                    .OrderByDescending(x => x.TotalDuration)
                    .ToList();

                var userTaskAmounts = allTimeEntries
                    .GroupBy(te => te.UserId)
                    .Select(g => new { UserId = g.Key, TaskCount = g.Select(te => te.LocalTaskId).Distinct().Count() })
                    .OrderByDescending(x => x.TaskCount)
                    .ToList();

                report.BestTime = userDurations.FirstOrDefault()?.TotalDuration ?? 0;
                report.WorstTime = userDurations.LastOrDefault()?.TotalDuration ?? 0;
                report.BestTimeUserId = userDurations.FirstOrDefault()?.UserId;
                report.WorstTimeUserId = userDurations.LastOrDefault()?.UserId;
                report.BestTaskAmount = userTaskAmounts.FirstOrDefault()?.TaskCount ?? 0;
                report.WorstTaskAmount = userTaskAmounts.LastOrDefault()?.TaskCount ?? 0;
                report.BestTaskAmountUserId = userTaskAmounts.FirstOrDefault()?.UserId;
                report.WorstTaskAmountUserId = userTaskAmounts.LastOrDefault()?.UserId;

                report.BestTimeUserName = (report.BestTimeUserId != null ? (await _userRepository.GetUserById(report.BestTimeUserId))?.DisplayName : "N/A") ?? "N/A";
                report.WorstTimeUserName = (report.WorstTimeUserId != null ? (await _userRepository.GetUserById(report.WorstTimeUserId))?.DisplayName : "N/A") ?? "N/A";
            }
            else
            {
                report.BestTime = report.WorstTime = 0;
                report.BestTaskAmount = report.WorstTaskAmount = 0;
                report.BestTimeUserId = report.WorstTimeUserId = null;
                report.BestTaskAmountUserId = report.WorstTaskAmountUserId = null;
                report.BestTimeUserName = report.WorstTimeUserName = "N/A";
            }

            return report;
        }

        public async Task<TaskReport> GetTaskReportAsync(int taskId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            if (taskId < 0)
            {
                throw new ArgumentException("Invalid Task ID.");
            }
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                var temp = startDate;
                startDate = endDate;
                endDate = temp;
            }
            var query = _dbContext.TimeEntries.AsQueryable()
                .Where(te => te.LocalTaskId == taskId);

            if (startDate.HasValue)
            {
                var utcStart = startDate.Value.UtcDateTime;
                query = query.Where(te => te.StartTime >= utcStart);
            }

            if (endDate.HasValue)
            {
                var utcEnd = endDate.Value.UtcDateTime;
                query = query.Where(te => te.EndTime <= utcEnd);
            }

            var allTimeEntries = await query
                .Select(te => new
                {
                    te.LocalTaskId,
                    te.UserId,
                    te.StartTime,
                    te.EndTime,
                    te.Duration
                })
                .ToListAsync();

            var report = new TaskReport
            {
                LocalTaskId = taskId,
                TotalHours = allTimeEntries.Sum(te => te.Duration.TotalHours),
                TotalEntries = allTimeEntries.Count,
                TotalUsers = allTimeEntries.Select(te => te.UserId).Distinct().Count(),
                TaskName = _dbContext.Tasks.FirstOrDefault(t => t.Id == taskId)?.Title ?? "Unknown Task"
            };

            if (allTimeEntries.Count > 0)
            {
                var userDurations = allTimeEntries
                    .GroupBy(te => te.UserId)
                    .Select(g => new { UserId = g.Key, TotalDuration = g.Sum(te => te.Duration.TotalHours) })
                    .OrderByDescending(x => x.TotalDuration)
                    .ToList();

                report.BestTime = userDurations.FirstOrDefault()?.TotalDuration ?? 0;
                report.WorstTime = userDurations.LastOrDefault()?.TotalDuration ?? 0;
                report.BestTimeUserId = userDurations.FirstOrDefault()?.UserId;
                report.WorstTimeUserId = userDurations.LastOrDefault()?.UserId;
                
                report.BestTimeUserName = (report.BestTimeUserId != null ? (await _userRepository.GetUserById(report.BestTimeUserId))?.DisplayName : "N/A") ?? "N/A";
                report.WorstTimeUserName = (report.WorstTimeUserId != null ? (await _userRepository.GetUserById(report.WorstTimeUserId))?.DisplayName : "N/A") ?? "N/A";
            }
            else
            {
                report.BestTime = report.WorstTime = 0;
                report.BestTimeUserId = report.WorstTimeUserId = null;
                report.BestTimeUserName = report.WorstTimeUserName = "N/A";
            }

            return report;
        }

        public string GenerateUserReportMessage(UserReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📊 **Report for user: {report.UserId}**");
            sb.AppendLine($"---");
            sb.AppendLine($"Total hours registered: **{report.TotalHours:F2}**.");
            sb.AppendLine($"Total amount of time entries: **{report.TotalEntries}**.");
            sb.AppendLine($"Total number of different tasks: **{report.TotalTasks}**.");
            sb.AppendLine($"---");

            if (report.TotalEntries == 0)
            {
                sb.AppendLine("No time entries found.");
            }

            return sb.ToString();
        }

        public string GenerateTaskReportMessage(TaskReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📋 **Report for task: {report.TaskName} ({report.LocalTaskId})**");
            sb.AppendLine($"---");
            sb.AppendLine($"Total hours spent: **{report.TotalHours:F2}**.");
            sb.AppendLine($"Total amount of time entries: **{report.TotalEntries}**.");
            sb.AppendLine($"Total number of different users: **{report.TotalUsers}**.");
            sb.AppendLine($"---");
            sb.AppendLine($"🏆 Best time: **{report.BestTime:F2}h** by {report.BestTimeUserName} ({report.BestTimeUserId ?? "N/A"}).");
            sb.AppendLine($"🐌 Worst time: **{report.WorstTime:F2}h** by {report.WorstTimeUserName} ({report.WorstTimeUserId ?? "N/A"}).");
            sb.AppendLine($"---");

            if (report.TotalEntries == 0)
            {
                sb.AppendLine("No time entries found.");
            }

            return sb.ToString();
        }

        public string GenerateOverallReportMessage(OverallReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🌍 **Overall Report**");
            sb.AppendLine($"---");
            sb.AppendLine($"Total hours registered **{report.TotalHours:F2}**.");
            sb.AppendLine($"Total amount of time entries: **{report.TotalEntries}**.");
            sb.AppendLine($"Total number of different tasks: **{report.TotalTasks}**.");
            sb.AppendLine($"---");

            if (report.TotalEntries == 0)
            {
                sb.AppendLine("No time entries found.");
                return sb.ToString();
            }

            sb.AppendLine("User statistics:");
            sb.AppendLine($"- Best time ({report.BestTime:F2}h): User **{report.BestTimeUserName ?? "N/A"}** ({report.BestTimeUserId ?? "N/A"}).");
            sb.AppendLine($"- Worst time ({report.WorstTime:F2}h): User **{report.WorstTimeUserName ?? "N/A"}** ({report.WorstTimeUserId ?? "N/A"}).");
            sb.AppendLine($"- Most tasks ({report.BestTaskAmount}): User **{report.BestTaskAmountUserId ?? "N/A"}**.");
            sb.AppendLine($"- Fewest tasks ({report.WorstTaskAmount}): User **{report.WorstTaskAmountUserId ?? "N/A"}**.");
            sb.AppendLine($"---");

            return sb.ToString();
        }
    }
}
