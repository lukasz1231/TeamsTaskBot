using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Common.Services
{
    public interface IGraphMetadataService
    {

        // users
        public Task<User> GetUserMetadataAsync(string userId);
        // messages
        public Task<ChatMessage> GetMessageMetadataAsync(string teamId, string channelId, string messageId);
        // tasks
        public Task<PlannerTask> GetTaskMetadataAsync(string taskId);
        public Task<Microsoft.Graph.Models.ChatMessage> GetReplyMetadataAsync(string teamId, string channelId, string messageId, string replyId);
        
        }
    public class GraphMetadataService : IGraphMetadataService
    {
        private readonly GraphServiceClient _graphClient;

        public GraphMetadataService(GraphServiceClient graphClient)
        {
            _graphClient = graphClient;
        }

        // Getting a specific user metadata by ID
        public async Task<Microsoft.Graph.Models.User> GetUserMetadataAsync(string userId)
        {
            return await _graphClient.Users[userId].GetAsync();
        }

        // Getting metadata of a specific message in a channel
        public async Task<Microsoft.Graph.Models.ChatMessage> GetMessageMetadataAsync(string teamId, string channelId, string messageId)
        {
            return await _graphClient.Teams[teamId].Channels[channelId].Messages[messageId].GetAsync();
        }


        public async Task<Microsoft.Graph.Models.ChatMessage?> GetReplyMetadataAsync(
            string teamId, string channelId, string messageId, string replyId)
        {
            var replies = await _graphClient.Teams[teamId]
                .Channels[channelId]
                .Messages[messageId]
                .Replies
                .GetAsync();

            // find reply with given ID on list
            return replies?.Value?.FirstOrDefault(r => r.Id == replyId);
        }
        // Getting metadata of a specific task by ID
        public async Task<PlannerTask> GetTaskMetadataAsync(string taskId)
        {
            return await _graphClient.Planner.Tasks[taskId].GetAsync();
        }
    }
}
