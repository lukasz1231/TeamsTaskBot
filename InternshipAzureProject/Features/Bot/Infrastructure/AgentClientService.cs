using Azure.AI.Agents.Persistent;
using Common.Data;
using Common.Options;
using Domain.Entities.Models;
using Domain.Interfaces;
using Microsoft.Extensions.Options;

public class AgentClientService : IAgentClientService
{
    private readonly PersistentAgentsClient _client;
    private readonly string _agentId;
    private readonly ILogger<AgentClientService> _logger;
    private readonly PersistentAgent _agent;
    private readonly AppDbContext _dbContext;

    public AgentClientService(
        PersistentAgentsClient client,
        IOptions<AzureAiOptions> options,
        ILogger<AgentClientService> logger,
        AppDbContext context)
    {
        _client = client;
        _logger = logger;
        _agentId = options.Value.AgentId
             ?? throw new InvalidOperationException("There is no AgentId in configuration.");
        _agent = _client.Administration.GetAgent(_agentId).Value
            ?? throw new InvalidOperationException($"Agent {_agentId} not found.");
        _dbContext = context;
    }

    public async Task<string?> GetResponseAsync(string messageThreadId, string message, DateTimeOffset? currentDate, CancellationToken cancellationToken = default)
    {
        try
        {
            string threadId;
            PersistentAgentThread thread;
            var userSession = await _dbContext.ThreadSessions.FindAsync(messageThreadId);
            if (userSession == null)
            {
                thread = await _client.Threads.CreateThreadAsync();
                threadId = thread.Id;
                userSession = new ThreadSessionModel
                {
                    MessageThreadId = messageThreadId,
                    ThreadId = threadId,
                    LastActivity = DateTime.UtcNow
                };
                _dbContext.ThreadSessions.Add(userSession);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                threadId = userSession.ThreadId;
                try
                {
                    thread = _client.Threads.GetThread(threadId).Value;
                    userSession.LastActivity = DateTime.UtcNow;
                }
                catch (Azure.RequestFailedException ex) when (ex.Status == 404)
                {
                    _dbContext.ThreadSessions.Remove(userSession);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    thread = await _client.Threads.CreateThreadAsync();
                    threadId = thread.Id;

                    userSession = new ThreadSessionModel
                    {
                        MessageThreadId = messageThreadId,
                        ThreadId = threadId,
                        LastActivity = DateTimeOffset.UtcNow
                    };
                    _dbContext.ThreadSessions.Add(userSession);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            // make sure there is no active run
            var runs = _client.Runs.GetRuns(threadId, order: ListSortOrder.Descending);
            ThreadRun? activeRun = runs.FirstOrDefault(r => r.Status == RunStatus.Queued || r.Status == RunStatus.InProgress);

            while (activeRun != null && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Thread {ThreadId} has an active run with status {Status}. Waiting...", threadId, activeRun.Status);
                await Task.Delay(1000, cancellationToken);
                activeRun = await _client.Runs.GetRunAsync(threadId, activeRun.Id);
                if (activeRun.Status != RunStatus.Queued && activeRun.Status != RunStatus.InProgress)
                {
                    activeRun = null;
                }
            }

            // create user message
            string contextualMessage = $"Current date and time is: {currentDate}. User's request: {message}";
            _client.Messages.CreateMessage(threadId, MessageRole.User, contextualMessage);

            ThreadRun run = await _client.Runs.CreateRunAsync(thread, _agent);

            // wait for run to stop
            var timeout = TimeSpan.FromSeconds(30);
            var start = DateTime.UtcNow;
            var delay = 500;

            while ((run.Status.Equals(RunStatus.Queued) || run.Status.Equals(RunStatus.InProgress))
                   && !cancellationToken.IsCancellationRequested
                   && DateTime.UtcNow - start < timeout)
            {
                await Task.Delay(delay, cancellationToken);
                var response = await _client.Runs.GetRunAsync(thread.Id, run.Id, cancellationToken);
                run = response.Value;
                delay = Math.Min(delay * 2, 5000);
            }

            if (run.Status != RunStatus.Completed)
            {
                _logger.LogWarning("Run failed with status {Status}", run.Status);
                return null;
            }

            var msgs = _client.Messages
                .GetMessages(thread.Id, order: ListSortOrder.Descending)
                .Where(m => m.Role == MessageRole.Agent)
                .ToList();

            var assistantMsg = msgs.FirstOrDefault();
            if (assistantMsg == null)
            {
                _logger.LogWarning("No new agent messages found for thread {ThreadId}", threadId);
                return null;
            }

            var text = assistantMsg.ContentItems
                .OfType<MessageTextContent>()
                .Select(t => t.Text)
                .FirstOrDefault();

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection error with agent OpenAI.");
            return null;
        }
    }
}
