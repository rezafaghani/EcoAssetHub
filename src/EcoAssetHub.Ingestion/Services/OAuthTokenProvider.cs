using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace EcoAssetHub.Ingestion.Services;

public class OAuthTokenProvider(HttpClient httpClient, IConfiguration configuration)
{
    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && _expiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _accessToken;
        }

        var tokenEndpoint = configuration["InsertApi:TokenEndpoint"];
        var clientId = configuration["InsertApi:ClientId"];
        var clientSecret = configuration["InsertApi:ClientSecret"];
        var scope = configuration["InsertApi:Scope"] ?? "ecoassethub.insert.write";

        if (string.IsNullOrWhiteSpace(tokenEndpoint) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            return string.Empty;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["scope"] = scope
            })
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        _accessToken = token?.AccessToken ?? string.Empty;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(token?.ExpiresIn ?? 300, 60));
        return _accessToken;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
