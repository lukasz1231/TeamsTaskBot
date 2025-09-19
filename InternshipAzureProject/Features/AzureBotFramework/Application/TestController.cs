using AdaptiveCards;
using Common.Options;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic; // Dodaj ten using
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace InternshipAzureProject.Features.Bot
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IBotService _botService;
        private readonly IAdaptiveCardService _adaptiveCardService;
        private readonly IOptions<DefaultValuesOption> _options;
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;
        public TestController(IBotService botService, IAdaptiveCardService adaptiveCardService, IOptions<DefaultValuesOption> options, IBot bot, IBotFrameworkHttpAdapter adapter)
        {
            _botService = botService;
            _adaptiveCardService = adaptiveCardService;
            _options = options;
            _bot = bot;
            _adapter = adapter;

        }

        // Metoda do wysyłania karty powitalnej (bez zmian)
        [HttpPost("welcome")]
        public async Task<IActionResult> PostWelcomeCard()
        {
            var teamId = _options.Value.teamId;
            var channelId = _options.Value.channelId;
            var welcomeCard = _adaptiveCardService.CreateWelcomeCard();

            try
            {
                // Użyj SendMessageAsync, jeśli wysyłasz nową konwersację
                await _botService.ReplyToMessageAsync(teamId, channelId, "1758137583169", "Welcome!", welcomeCard);

                return Ok("Welcome card sent successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while sending the card: {ex.Message}");
            }
        }

        // Nowy endpoint do odbierania danych z przycisków Adaptive Card
        [HttpPost("/api/webhook/get/azurebot")]
        public async Task PostAsync()
        {
            await _adapter.ProcessAsync(Request, Response, _bot);
        }
    }
    public class AdaptiveCardSubmitData
    {
        public string Action { get; set; }
    }
}
