using Domain.Entities.Models;

namespace Domain.Interfaces
{
    public interface IUserGraphService
    {
        public Task<List<UserModel>> GetAllAsync();
        public Task<List<string?>> GetUserRolesAsync(string userId);
        public Task<string?> GetAzureAdObjectIdFromTeamsIdAsync(string teamsUserId);
    }
}
