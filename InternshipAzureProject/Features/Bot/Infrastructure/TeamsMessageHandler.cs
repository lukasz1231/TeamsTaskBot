using Common.Options;
using Common.Services;
using Domain.Entities.Dtos;
using Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Infrastructure.Services
{
    public class TeamsMessageHandler : ITeamsMessageHandler
    {
        private readonly IGraphMetadataService _graphMetadataService;
        private readonly IMessageParser _parser;
        private static readonly HashSet<string> _processedMessages = new();
        private readonly IOptions<BotOptions> _botOptions;
        private readonly ILogger<TeamsMessageHandler> _logger;
        private readonly IActionHandler _actionHandler;
        public TeamsMessageHandler(IGraphMetadataService graphMetadataService, IMessageParser parser, IOptions<BotOptions> options, ILogger<TeamsMessageHandler> logger, IActionHandler actionHandler)
        {
            _graphMetadataService = graphMetadataService;
            _parser = parser;
            _botOptions = options;
            _logger = logger;
            _actionHandler = actionHandler;
        }
        public async Task HandleMessageAsync(string notificationBody)
        {
            var regex = new Regex(@"teams\('([\w\d-]+)'\)/channels\('([^']+)'\)/messages\('(\d+)'\)(?:/replies\('(\d+)'\))?");

            var matches = regex.Matches(notificationBody);


            // process each match
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var teamId = match.Groups[1].Value;
                    var channelId = match.Groups[2].Value;

                    string messageId = match.Groups[3].Value;
                    string replyId = match.Groups[4].Success ? match.Groups[4].Value : null;
                    _logger.LogInformation(@"MESSAGEID = {id}", messageId);
                    _logger.LogInformation(@"REPLYID = {id}", replyId);

                    Microsoft.Graph.Models.ChatMessage message;
                    if (replyId != null)
                    {
                        _logger.LogInformation("Processing reply teamId={teamId}, channelId = {channelId}, messageId= {messageId}, replyId = {ReplyId} in message", teamId, channelId, messageId, replyId);
                        message = await _graphMetadataService.GetReplyMetadataAsync(teamId, channelId, messageId, replyId);
                    }
                    else
                    {
                        // get thread message
                        message = await _graphMetadataService.GetMessageMetadataAsync(teamId, channelId, messageId);
                    }
                    if (message == null)
                    {
                        _logger.LogWarning("Reply {ReplyId} not found in message {MessageId}", replyId, messageId);
                        continue;
                    }
                    if (replyId == null)
                    {

                        if (_processedMessages.Contains(messageId))
                        {
                            _logger.LogInformation("Skipping already processed message {MessageId}", messageId);
                            continue; // Skip already processed messages
                        }
                        _processedMessages.Add(messageId); // Mark message as processed
                    }
                    else { 
                    if (_processedMessages.Contains(replyId))
                    {
                        _logger.LogInformation("Skipping already processed reply {MessageId}", replyId);
                        continue; // Skip already processed messages
                    }
                    _processedMessages.Add(replyId); // Mark message as processed
                        if (!_processedMessages.Contains(messageId))
                        {
                            _processedMessages.Add(messageId); // Mark parent message as processed too
                        }
                    }
                    var messageDto = new TeamsMessageDto
                    {
                        Id = message.Id,
                        Title = message?.Subject,
                        FromUserId = message.From?.User?.Id,
                        ReplyToId = message.ReplyToId??message.Id, //if it's not a reply, set ReplyToId to its own Id
                        FromDisplayName = message.From?.User?.DisplayName,
                        Body = Regex.Replace(message.Body?.Content, "<.*?>", string.Empty),
                        CreatedDateTime = message.CreatedDateTime
                    };
                    _logger.LogInformation("FromUserId={FromUserId}, BotId={BotId}", messageDto.FromUserId, _botOptions.Value.BotId);
                    if (messageDto.FromUserId == _botOptions.Value.BotId)
                    {
                        _logger.LogInformation("Skipping message {MessageId} as it was sent by the bot itself", messageDto.Body);
                        continue; // Skip processing message sent by the bot itself
                    }

                    if(messageDto.ReplyToId == messageDto.Id)
                    {
                        if(messageDto.Title != "BOT")
                            continue; // Skip processing message not titled "BOT" (only for root messages)
                    }
                    else
                    {
                        var parentMessage = await _graphMetadataService.GetMessageMetadataAsync(teamId, channelId, messageDto.ReplyToId);
                        if (parentMessage.Subject != "BOT")
                        {
                            _logger.LogInformation("Skipping reply {ReplyId} as its parent message {ParentId} is not a bot thread", messageDto.Id, messageDto.ReplyToId);
                            continue; // Skip processing replies not in bot thread
                        }
                    }


                    // 1. Parse message
                    var parsed = await _parser.Parse(messageDto.ReplyToId, messageDto.Body, messageDto.CreatedDateTime.Value.ToOffset(new TimeSpan(2, 0, 0)));
                    // 2. Handle action
                    await _actionHandler.HandleActionAsync(parsed, messageDto, teamId, channelId);
                }
            }
        }


    }
}