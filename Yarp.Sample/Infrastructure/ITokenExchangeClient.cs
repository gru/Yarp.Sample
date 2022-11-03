namespace Yarp.Sample.Infrastructure;

public interface ITokenExchangeClient
{
    Task<string> ExchangeToken(string accessToken);
}