using System.Net;

namespace Concertable.Kernel.Identity;

public interface IClientContext
{
    IPAddress IpAddress { get; }
    string? UserAgent { get; }
}
