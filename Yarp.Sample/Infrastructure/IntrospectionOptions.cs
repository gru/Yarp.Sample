using Microsoft.IdentityModel.Tokens;

namespace Yarp.Sample.Infrastructure;

public class IntrospectionOptions
{
    public IntrospectionOptions()
    {
        Paths = Array.Empty<string>();
    }
    
    public string[] Paths { get; set; }
}