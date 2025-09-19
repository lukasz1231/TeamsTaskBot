using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Common.Options;

namespace Api.Controllers
{
    [ApiController]
    public class TeamsWebhookController : ControllerBase
        {
        private readonly ISubscriptionService _subscribtionRepository;
        private readonly IGraphMetadataService _graphMetadataService;
        private readonly ILogger<TeamsWebhookController> _logger;
        private readonly ITeamsMessageHandler _teamsMessageHandler;
        private readonly IOptions<DefaultValuesOption> _options;
        public TeamsWebhookController(ISubscriptionService subscribtionRepository, ILogger<TeamsWebhookController> logger, IGraphMetadataService graphMetadataService, ITeamsMessageHandler teamsMessageHandler, IOptions<DefaultValuesOption> options)
        {
            _subscribtionRepository = subscribtionRepository;
            _logger = logger;
            _graphMetadataService = graphMetadataService;
            _teamsMessageHandler = teamsMessageHandler;
            _options = options;
        }
        [HttpPost]
        [Route("/api/webhook/get")]
        public async Task<IActionResult> HandleWebhook([FromQuery(Name = "validationToken")] string? validationToken)
        {
            // check if the request contains a validation token to verify the webhook subscription
            if (!string.IsNullOrEmpty(validationToken))
            {
                _logger.LogInformation("Received webhook validation request.");
                return Content(validationToken, "text/plain");
            }

            // No token provided (subscription already exists), proceed with processing the webhook notification.
            try
            {
                // reading the body of the request
                string body = string.Empty;
                if (Request.Body.CanRead && Request.ContentLength > 0)
                {
                    using var reader = new StreamReader(Request.Body);
                    body = await reader.ReadToEndAsync();
                }
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("Received webhook with empty body.");
                    return Ok();
                }
                await _teamsMessageHandler.HandleMessageAsync(body);


                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook notification.");
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("/api/webhook/post")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Post()
        {
            var teamId = _options.Value.teamId;
            var channelId = _options.Value.channelId;
            var existing = await _subscribtionRepository.GetSubscriptionsAsync();
            if (existing == null)
            {
                var newSubscription = await _subscribtionRepository.CreateSubscriptionAsync(teamId, channelId);
                return Ok(newSubscription);
            }
            else
            {
                return Conflict(new { message = "Subscription already exists." });
            }

        }
    }

}
