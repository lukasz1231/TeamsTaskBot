using Domain.Entities.Models;
using Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Common.Options;

namespace Infrastructure.Services
{
    public class UserGraphService : IUserGraphService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly IOptions<AzureOptions> _options;

        public UserGraphService(GraphServiceClient graphClient, IOptions<AzureOptions> options)
        {
            _graphClient = graphClient;
            _options = options;
        }



        public async Task<List<UserModel>> GetAllAsync()
        {
            var users = new List<UserModel>();
            var page = await _graphClient.Users.GetAsync(config =>
            {
                config.QueryParameters.Select = new[] { "id", "displayName", "userPrincipalName" };
                config.QueryParameters.Top = 999;
            });

            while (page != null)
            {
                if (page.Value != null)
                {
                    users.AddRange(page.Value.Select(u => new UserModel
                    {
                        UserId = u.Id,
                        DisplayName = u.DisplayName,
                        Email = u.UserPrincipalName
                    }));
                }

                if (page.OdataNextLink != null)
                {
                    page = await _graphClient.Users.WithUrl(page.OdataNextLink).GetAsync(config =>
                    {
                        config.QueryParameters.Top = 999;
                    });
                }
                else
                {
                    page = null;
                }
            }

            return users;
        }



        public async Task<List<string?>> GetUserRolesAsync(string userId)
        {
            // Get the user's roles
            var roleAssignments = await _graphClient.Users[userId].AppRoleAssignments.GetAsync();

            if (roleAssignments?.Value == null) return new List<string?>();

            // Get the Service Principal with the required properties
            var servicePrincipalResponse = await _graphClient.ServicePrincipals
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = $"appId eq '{_options.Value.ClientId}'";
                        requestConfiguration.QueryParameters.Select = new[] { "appRoles", "displayName" };
                    });
            var servicePrincipal = servicePrincipalResponse?.Value?.FirstOrDefault();
            var appRoles = servicePrincipal?.AppRoles;

            if (appRoles == null) return new List<string?>();

            // Join the user's roles with the app's defined roles
            var roles = roleAssignments.Value
                .Where(ra => ra.ResourceDisplayName == servicePrincipal?.DisplayName)
                .Join(
                    appRoles,
                    ra => ra.AppRoleId,
                    ar => ar.Id,
                    (ra, ar) => ar.Value
                )
                .ToList();

            return roles;
        }

        public async Task<string?> GetAzureAdObjectIdFromTeamsIdAsync(string teamsUserId)
        {
            try
            {
                // W nowym SDK używamy GetAsync z konfiguracją
                var users = await _graphClient.Users
                    .GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.Filter =
                            $"identities/any(c:c/issuerAssignedId eq '{teamsUserId}' and c/issuer eq 'live.com')";
                    });

                var user = users?.Value?.FirstOrDefault();
                return user?.Id; // Azure AD ObjectId
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Graph API error: {ex.Message}");
                return null;
            }
        }
    }
}
