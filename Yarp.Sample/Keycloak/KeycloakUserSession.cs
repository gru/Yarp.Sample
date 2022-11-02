using System.Text.Json.Serialization;

namespace Yarp.Sample.Keycloak;

public class KeycloakUserSession
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("start")]
    public long Start { get; set; }
}