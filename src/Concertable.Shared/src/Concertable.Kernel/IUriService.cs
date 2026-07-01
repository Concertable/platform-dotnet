namespace Concertable.Kernel;

public interface IUriService
{
    Uri GetUri(string path, IDictionary<string, string>? query = null);
}
