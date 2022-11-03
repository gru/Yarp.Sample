using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Yarp.Sample.Infrastructure;

public class TokenExchangeClient :  ITokenExchangeClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<YarpSampleOptions> _optionsMonitor;

    public TokenExchangeClient(HttpClient httpClient, IOptionsMonitor<YarpSampleOptions> optionsMonitor)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
    }

    public async Task<string> ExchangeToken(string accessToken)
    {
        var options = _optionsMonitor.CurrentValue;
        
        var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
                ["subject_token"] = accessToken,
                ["requested_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
                ["audience"] = options.Audience,
            });
        
        var response = await _httpClient.PostAsync("/realms/yarp/protocol/openid-connect/token", content);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TokenExchangeResult>(json)!;

        return result.AccessToken;
    }

    private class TokenExchangeResult
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
}