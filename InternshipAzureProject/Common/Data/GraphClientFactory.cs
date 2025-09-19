using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Common.Options;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Text.Json;

public class RefreshTokenProvider : IAuthenticationProvider
{
    private readonly IOptions<AzureOptions> _cfg;
    private string _accessToken;
    private DateTimeOffset _tokenExpiresOn;

    public RefreshTokenProvider(IOptions<AzureOptions> cfg)
    {
        _cfg = cfg;
    }

    public async Task AuthenticateRequestAsync(HttpRequestMessage request)
    {
        // Check if token expired or doesn't exist
        if (string.IsNullOrEmpty(_accessToken) || _tokenExpiresOn < DateTimeOffset.Now.AddMinutes(5))
        {
            await GetNewAccessTokenAsync();
        }

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
    }

    public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_accessToken) || _tokenExpiresOn < DateTimeOffset.Now.AddMinutes(5))
        {
            await GetNewAccessTokenAsync();
        }

        request.Headers["Authorization"] = new[] { $"Bearer {_accessToken}" };
    }

    private async Task GetNewAccessTokenAsync()
    {
        var client = new HttpClient();
        var options = _cfg.Value;

        var requestBody = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string, string>("grant_type", "refresh_token"),
        new KeyValuePair<string, string>("client_id", options.ClientId),
        new KeyValuePair<string, string>("client_secret", options.ClientSecret),
        new KeyValuePair<string, string>("refresh_token", options.RefreshToken)
    });

        var response = await client.PostAsync(
            $"https://login.microsoftonline.com/{options.TenantId}/oauth2/v2.0/token",
            requestBody
        );

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);

        _accessToken = tokenResponse.GetProperty("access_token").GetString()!;
        var expiresInSeconds = tokenResponse.GetProperty("expires_in").GetInt64();
        _tokenExpiresOn = DateTimeOffset.Now.AddSeconds(expiresInSeconds);
    }
}

namespace Common.Data
{
    public static class GraphClientFactory
    {
        public static GraphServiceClient Create(IOptions<AzureOptions> options)
        {
            var cfg = options.Value;

            var credential = new ClientSecretCredential(
                cfg.TenantId,
                cfg.ClientId,
                cfg.ClientSecret
            );

            return new GraphServiceClient(
                credential,
                new[] { "https://graph.microsoft.com/.default" }
            );
        }

        /// <summary>
        /// Creates a GraphServiceClient with delegated permissions using a refresh token.
        /// </summary>
        public static GraphServiceClient CreateWithDelegatedPermissions(IOptions<AzureOptions> options)
        {
            var authProvider = new RefreshTokenProvider(options);
            return new GraphServiceClient(authProvider);
        }
        public static PersistentAgentsClient CreateAzureAiClient(IOptions<AzureOptions> options)
        {
            var cfg = options.Value;

            var credential = new ClientSecretCredential(
                cfg.TenantId,
                cfg.ClientId,
                cfg.ClientSecret
            );

            return new PersistentAgentsClient(cfg.Endpoint, credential);
        }
    }
}

