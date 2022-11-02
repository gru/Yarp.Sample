namespace Yarp.Sample.Keycloak;

public interface IKeycloakApi
{
    Task<KeycloakRealmSettings> GetRealmSettings();
    
    Task<KeycloakUserSession[]> GetUserSessions(string user);
    
    Task LogoutUser(string user);
}