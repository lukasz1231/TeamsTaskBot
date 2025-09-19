using Domain.Entities;
namespace Domain.Interfaces
{
    public interface ITeamsMessageHandler
    {
        Task HandleMessageAsync(string notificationBody);
    }
}
