namespace Common.Options
{
    public class AzureOptions
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
