using System.Text.Json.Serialization;

namespace Yarp.Sample.Keycloak;

public class KeycloakRealmSettings
{
    [JsonPropertyName("ssoSessionMaxLifespan")]
    public long? SsoSessionMax { get; set; }
    
    [JsonPropertyName("clientSessionMaxLifespan")]
    public long? ClientSessionMax { get; set; }
}