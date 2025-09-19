using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Application.Services
{
    public class OpenAIMessageParser : IMessageParser
    {
        private readonly IAgentClientService _agent;
        private readonly ILogger<OpenAIMessageParser> _logger;

        public OpenAIMessageParser(IAgentClientService agent, ILogger<OpenAIMessageParser> logger)
        {
            _agent = agent;
            _logger = logger;
        }

        public async Task<ParsedMessageDto> Parse(string messageThreadId, string message, DateTimeOffset? currentDate)
        {
            var rawResponse = await _agent.GetResponseAsync(messageThreadId, message, currentDate);
            _logger.LogInformation("Agent raw response: {RawResponse}", rawResponse);
            if (string.IsNullOrWhiteSpace(rawResponse))
                return Fallback(message, "No agent response.");

            var json = ExtractJson(rawResponse);
            if (string.IsNullOrWhiteSpace(json))
                return Fallback(message, "JSON not found in response.", rawResponse);

            AgentParsePayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<AgentParsePayload>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON deserialization error.");
                return Fallback(message, "JSON deserialization error.", json);
            }

            if (payload is null)
                return Fallback(message, "Empty payload.", json);

            var action = MapIntentToAction(payload.Action);

            List<int>? numbers = payload.ParsedNumbers;

            return new ParsedMessageDto(
                action: action,
                content: payload.Content,
                reply: payload.Reply
            )
            {
                ParsedNumbers = numbers,
                TaskName = payload.TaskName,
                PercentComplete = payload.PercentComplete,
                Time = payload.Time,
                NewName = payload.NewName,
                StartDate = payload.StartDate,
                EndDate = payload.EndDate,
                userIds = payload.userIds
            };
        }
        // --- helpers ---

        private static string? ExtractJson(string text)
        {
            var fenced = Regex.Match(text, "```(?:json)?\\s*(\\{[\\s\\S]*\\})\\s*```", RegexOptions.IgnoreCase);
            if (fenced.Success) return fenced.Groups[1].Value.Trim();

            int first = text.IndexOf('{');
            int last = text.LastIndexOf('}');
            if (first >= 0 && last > first)
            {
                return text.Substring(first, last - first + 1).Trim();
            }
            return null;
        }

        private ParsedMessageDto Fallback(string userMessage, string reason, string? raw = null)
        {
            _logger.LogWarning("Fallback triggered. Reason: {Reason}. Raw: {Raw}", reason, raw);
            return new ParsedMessageDto(ActionType.Smalltalk, null, userMessage,
                "I couldn’t understand your message. Could you try again?");
        }

        private static ActionType MapIntentToAction(string? actionFromModel)
        {
            if (string.IsNullOrWhiteSpace(actionFromModel))
                return ActionType.Unknown;

            return Enum.TryParse<ActionType>(actionFromModel.Trim(), ignoreCase: true, out var action)
                ? action
                : ActionType.Unknown;
        }

        private sealed class AgentParsePayload
        {
            public string? Action { get; set; }
            public string? TaskName { get; set; }
            public string? Content { get; set; }
            public List<int>? ParsedNumbers { get; set; }
            public string? Reply { get; set; }
            public int? PercentComplete { get; set; }
            public string? NewName { get; set; }
            public double? Time { get; set; }
            public DateTimeOffset? StartDate { get; set; }
            public DateTimeOffset? EndDate { get; set; }
            public List<string>? userIds { get; set; }
            }
    }
}
