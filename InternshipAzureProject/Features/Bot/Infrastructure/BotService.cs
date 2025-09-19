using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using Common.Options;
using Domain.Interfaces;
using Common.Data;
using Microsoft.Extensions.Options;

public class BotService : IBotService
{
    private readonly IOptions<AzureOptions> _cfg;

    public BotService(IOptions<AzureOptions> cfg)
    {
        _cfg = cfg;
    }

    /// <summary>
    /// Sends a text message to a specific channel.
    /// </summary>
    /// <param name="teamId">The ID of the team.</param>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="message">The text message to send.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task SendMessageAsync(string teamId, string channelId, string message)
    {
        var graphClient = GraphClientFactory.CreateWithDelegatedPermissions(_cfg);

        var chatMessage = new ChatMessage
        {
            Subject = "BOTMESSAGE", // always subject BOTMESSAGE
            Body = new ItemBody
            {
                Content = message
            }
        };

        await graphClient.Teams[teamId]
                         .Channels[channelId]
                         .Messages.PostAsync(chatMessage);
    }

    /// <summary>
    /// Replies to a message in a channel with either a text message or an Adaptive Card.
    /// </summary>
    /// <param name="teamId">The ID of the team where the reply should be sent.</param>
    /// <param name="channelId">The ID of the channel containing the parent message.</param>
    /// <param name="parentMessageId">The ID of the message to reply to.</param>
    /// <param name="message">An optional text message to be included in the reply.</param>
    /// <param name="adaptiveCard">An optional AdaptiveCard object to be sent as a reply.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task ReplyToMessageAsync(string teamId, string channelId, string parentMessageId, string? message = null, AdaptiveCard? adaptiveCard = null)
    {
        var graphClient = GraphClientFactory.CreateWithDelegatedPermissions(_cfg);
        var reply = new ChatMessage();

        if (adaptiveCard != null)
        {
            // Prepare the message with an Adaptive Card
            var attachmentId = Guid.NewGuid().ToString();
            var attachmentMarker = $"<attachment id=\"{attachmentId}\"></attachment>";

            reply.Body = new ItemBody
            {
                ContentType = BodyType.Html,
                // Include a text message if provided, followed by the card marker
                Content = !string.IsNullOrEmpty(message) ? message + "<br/>" + attachmentMarker : attachmentMarker
            };
            reply.Attachments = new List<ChatMessageAttachment>
            {
                new ChatMessageAttachment
                {
                    Id = attachmentId,
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    ContentUrl = null,
                    Content = adaptiveCard.ToJson()
                }
            };
        }
        else
        {
            // Prepare the message with plain text
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message), "Message content cannot be null or empty when no Adaptive Card is provided.");
            }
            reply.Body = new ItemBody
            {
                ContentType = BodyType.Text,
                Content = message
            };
        }

        await graphClient.Teams[teamId]
                         .Channels[channelId]
                         .Messages[parentMessageId]
                         .Replies
                         .PostAsync(reply);
    }

}