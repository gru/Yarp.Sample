namespace Yarp.Shared;

public interface IPassportService
{
    Task<Passport> Read(byte[] bytes, CancellationToken cancellationToken = default);

    Task<byte[]> Write(User user);
}