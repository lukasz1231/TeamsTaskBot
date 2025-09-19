using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;

namespace Infrastructure.Services
{
    public class BotAdapterWithErrorHandler : CloudAdapter
    {
        public BotAdapterWithErrorHandler(IConfiguration configuration)
            : base(new ConfigurationBotFrameworkAuthentication(configuration))
        {
            // Obsługa błędów na poziomie adaptera
            OnTurnError = async (turnContext, exception) =>
            {
                // Logowanie błędów
                Console.WriteLine($"[OnTurnError] unhandled error : {exception.Message}");

                // Wysyłanie wiadomości do użytkownika o błędzie
                await turnContext.SendActivityAsync("Przepraszam, coś poszło nie tak. Spróbuj ponownie.");
            };
        }
    }
}
