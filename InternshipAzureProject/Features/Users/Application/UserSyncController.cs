using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Domain.Interfaces;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles ="Admin")]
    public class UserSyncController : ControllerBase
    {
        private readonly ISyncService _userSyncService;

        public UserSyncController(ISyncService userSyncService)
        {
            _userSyncService = userSyncService;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncUsers()
        {
            try
            {
                await _userSyncService.SyncUsersAsync();
                return Ok("Sync completed!");
            }
            catch (Exception ex)
            {
                // error handling
                return StatusCode(500, $"Sync error: {ex.Message}");
            }
        }
    }
}
