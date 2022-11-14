using ProtoBuf;

namespace Yarp.Shared;

[ProtoContract]
public class User
{
    public User()
    {
    }
    
    public User(string userName)
    {
        UserName = userName;
    }
    
    [ProtoMember(1)]
    public string UserName { get; }
}