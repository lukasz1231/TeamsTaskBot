using Common.Data;
using Domain.Entities.Models;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserModel>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<UserModel> GetUserById(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new Exception($"User with id '{id}' not found.");
            }
            return user;
        }

        public async Task<List<UserModel>> GetUsersByNameAsync(string userName)
        {
            var normalizedName = NormalizeAsci(userName);

            var user = await _context.Users
                .Where(u => u.NormalizedDisplayName == normalizedName).ToListAsync();

            if (user == null)
                throw new Exception($"User with name '{userName}' not found.");

            return user;
        }

        private static string NormalizeAsci(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var normalized = name.ToLowerInvariant()
                                  .Replace(" ", "")
                                  .Replace("ą", "a")
                                  .Replace("ć", "c")
                                  .Replace("ę", "e")
                                  .Replace("ł", "l")
                                  .Replace("ń", "n")
                                  .Replace("ó", "o")
                                  .Replace("ś", "s")
                                  .Replace("ż", "z")
                                  .Replace("ź", "z");

            return normalized;
        }
    }
}
