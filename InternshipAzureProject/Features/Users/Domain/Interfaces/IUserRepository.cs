using Domain.Entities.Models;

namespace Domain.Interfaces
{
    public interface IUserRepository
    {
        public Task<UserModel> GetUserById(string id);
        public Task<List<UserModel>> GetAllAsync();
        public Task<List<UserModel>> GetUsersByNameAsync(string userName);
    }
}
