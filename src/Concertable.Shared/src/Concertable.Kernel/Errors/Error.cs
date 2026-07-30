namespace Concertable.Kernel.Errors;

public enum ErrorKind
{
    Invalid,
    NotFound,
    Conflict,
    Unauthenticated,
    Forbidden,
    PaymentRequired
}

public record ErrorDescriptor(
    string Code,
    string Message,
    ErrorKind Kind);

public sealed record ValidationErrorDescriptor(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]> Errors)
    : ErrorDescriptor(Code, Message, ErrorKind.Invalid);

public interface IError
{
    ErrorDescriptor Descriptor { get; }
}
