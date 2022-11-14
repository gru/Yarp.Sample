using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ProtoBuf;

namespace Yarp.Shared;

public class PassportService : IPassportService
{
    private readonly IOptions<PassportOptions> _options;

    public PassportService(IOptions<PassportOptions> options)
    {
        _options = options;
    }
    
    public Task<Passport> Read(byte[] bytes, CancellationToken cancellationToken = default)
    {
        var passport = Serializer.Deserialize<Passport>(bytes.AsSpan());
        
        using var userStream = new MemoryStream();
        Serializer.Serialize(userStream, passport.User);
        
        var key = Encoding.UTF8.GetBytes(_options.Value.Secret);
        
        using var hash = new HMACSHA256(key);
        var integrityBytes = hash.ComputeHash(userStream.ToArray());

        if (!integrityBytes.SequenceEqual(passport.Integrity.Signature))
            throw new PassportException("Signature not valid");
        
        return Task.FromResult(passport);
    }

    public Task<byte[]> Write(User user)
    {
        using var userStream = new MemoryStream();
        Serializer.Serialize(userStream, user);    
        
        var key = Encoding.UTF8.GetBytes(_options.Value.Secret);
        
        using var hash = new HMACSHA256(key);
        var integrityBytes = hash.ComputeHash(userStream.ToArray());
        var integrity = new Integrity(integrityBytes);
        var passport = new Passport(user, integrity);
            
        using var passportStream = new MemoryStream();
        Serializer.Serialize(passportStream, passport);
        return Task.FromResult(passportStream.ToArray());
    }
}