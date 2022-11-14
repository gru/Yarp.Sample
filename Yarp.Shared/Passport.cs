using ProtoBuf;

namespace Yarp.Shared;

[ProtoContract]
public class Passport
{
    public Passport()
    {
    }
    
    public Passport(User type, Integrity integrity)
    {
        User = type;
        Integrity = integrity;
    }

    [ProtoMember(1)]
    public User User { get; set; }
    
    [ProtoMember(2)]
    public Integrity Integrity { get; set; }
}