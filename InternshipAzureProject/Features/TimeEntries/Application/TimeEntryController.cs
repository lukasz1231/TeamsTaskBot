using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeEntryController : ControllerBase
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly ITaskFacade _taskFacade;
        private readonly IReportService _reportService;

        public TimeEntryController(
            ITimeEntryService timeEntryService,
            ITaskFacade taskFacade,
            IReportService reportService)
        {
            _timeEntryService = timeEntryService;
            _taskFacade = taskFacade;
            _reportService = reportService;
        }

        [HttpPost("user/{userId}/start/{taskId}")]
        [Authorize(Roles = "Admin,Task.TimeTracker")]
        public async Task<IActionResult> StartTask(string userId, int taskId)
        {
            try
            {
                await _timeEntryService.StartTaskById(userId, taskId);
                return CreatedAtAction(nameof(StartTask), new { userId, taskId });
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Task with ID {taskId} for user {userId} not found.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("user/{userId}/stop/{taskId}")]
        [Authorize(Roles = "Admin,Task.TimeTracker")]
        public async Task<IActionResult> StopTask(string userId, int taskId, [FromBody][Range(0, 100)] int percentComplete)
        {
            try
            {
                var entry = await _timeEntryService.StopTaskById(userId, taskId, percentComplete);
                if (entry == null)
                    return NotFound($"No active time entry found for task {taskId} and user {userId}.");
                var showEntry = new
                {
                    entry.Id,
                    entry.UserId,
                    entry.LocalTaskId,
                    entry.StartTime,
                    entry.EndTime,
                    DurationHours = entry.Duration.TotalHours,
                };
                return Ok(showEntry);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("user/{userId}/manual/{taskId}")]
        [Authorize(Roles = "Admin, Task.ManualTimeTracker")]
        public async Task<IActionResult> AddTimeToTask(string userId, int taskId, [FromBody] ManualTimeEntryDto timeEntryDto)
        {
            if (!timeEntryDto.Time.HasValue && (!timeEntryDto.StartTime.HasValue || !timeEntryDto.EndTime.HasValue))
            {
                return BadRequest("You must provide either 'Time' or both 'StartTime' and 'EndTime'.");
            }
            try
            {
                var entry = await _timeEntryService.AddTimeToTaskById(
                    userId,
                    taskId,
                    timeEntryDto.Time,
                    timeEntryDto.PercentComplete,
                    timeEntryDto.StartTime,
                    timeEntryDto.EndTime
                );

                return CreatedAtAction(nameof(AddTimeToTask), new { userId, taskId });

            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }


        [HttpGet("user/{userId}/report")]
        [Authorize(Roles = "Admin, Report.AdminViewer, Report.SelfViewer")]
        public async Task<IActionResult> GetUserReport(
            string userId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                return BadRequest("StartDate must be earlier than EndDate.");

            var report = await _reportService.GetUserReportAsync(userId, startDate, endDate);
            return Ok(report);
        }

        [HttpGet("task/{taskId}/report")]
        [Authorize(Roles = "Admin, Report.AdminViewer, Report.SelfViewer")]
        public async Task<IActionResult> GetTaskReport(
            int taskId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                return BadRequest("StartDate must be earlier than EndDate.");

            var report = await _reportService.GetTaskReportAsync(taskId, startDate, endDate);
            return Ok(report);
        }

        [HttpGet("overall/report")]
        [Authorize(Roles = "Admin, Report.AdminViewer")]
        public async Task<IActionResult> GetOverallReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                return BadRequest("StartDate must be earlier than EndDate.");

            var report = await _reportService.GetOverallReportAsync(startDate, endDate);
            return Ok(report);
        }

        public class ManualTimeEntryDto
        {
            [Range(0.01, double.MaxValue, ErrorMessage = "Time must be positive.")]
            public double? Time { get; set; }

            public DateTimeOffset? StartTime { get; set; }

            public DateTimeOffset? EndTime { get; set; }

            [Range(0, 100, ErrorMessage = "PercentComplete must be between 0 and 100.")]
            public int? PercentComplete { get; set; }
        }

    }
}
