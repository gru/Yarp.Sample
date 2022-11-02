using System.Text.Json;

namespace Yarp.Sample.Keycloak;

public class KeycloakApi : IKeycloakApi
{
    private readonly HttpClient _httpClient;

    public KeycloakApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public Task LogoutUser(string user)
    {
        var body = JsonContent.Create(new
        {
            realm = "yarp", 
            user = user
        });
        
        return _httpClient.PostAsync($"/admin/realms/yarp/users/{user}/logout", body);
    }

    public async Task<KeycloakRealmSettings> GetRealmSettings()
    {
        var response = await _httpClient.GetAsync($"/admin/realms/yarp");
        
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<KeycloakRealmSettings>(json)!;
    }

    public async Task<KeycloakUserSession[]> GetUserSessions(string user)
    {
        var response = await _httpClient.GetAsync($"/admin/realms/yarp/users/{user}/sessions");
        
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<KeycloakUserSession[]>(json)!;
    }
}