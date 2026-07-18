namespace Concertable.Kernel.ValueObjects;

public interface IAddress
{
    string County { get; }
    string Town { get; }
}
