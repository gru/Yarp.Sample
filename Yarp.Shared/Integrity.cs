using ProtoBuf;

namespace Yarp.Shared;

[ProtoContract]
public class Integrity
{
    public Integrity()
    {
    }
    
    public Integrity(byte[] signature)
    {
        Signature = signature;
    }

    [ProtoMember(1)]
    public byte[] Signature { get; set; }
}