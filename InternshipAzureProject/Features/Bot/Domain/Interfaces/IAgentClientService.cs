namespace Domain.Interfaces
{
    public interface IAgentClientService
    {
        Task<string?> GetResponseAsync(string userId, string message, DateTimeOffset? currentDate, CancellationToken cancellationToken = default);
    }
}
