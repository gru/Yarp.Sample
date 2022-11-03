namespace Yarp.Sample.Infrastructure;

public static class AppAuthenticationSchemes
{
    public const string IntrospectionScheme = "Introspection";
    
    public const string ValidationScheme = "Bearer";
    
    public const string CompoundScheme = $"{ValidationScheme}Or{IntrospectionScheme}";
}