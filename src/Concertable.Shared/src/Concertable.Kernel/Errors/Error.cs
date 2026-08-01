using System.Text.RegularExpressions;

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

public partial record ErrorDefinition(
    string Code,
    string Message,
    ErrorKind Kind)
{
    public static ErrorDefinition Invalid(string code, string message) =>
        new(code, message, ErrorKind.Invalid);

    public static ErrorDefinition NotFound(string code, string message) =>
        new(code, message, ErrorKind.NotFound);

    public static ErrorDefinition NotFound<T>(string code) =>
        NotFound(code, $"{DisplayNameResolver.Of<T>()} not found.");

    public static ErrorDefinition Conflict(string code, string message) =>
        new(code, message, ErrorKind.Conflict);

    public static ErrorDefinition Unauthenticated(string code, string message) =>
        new(code, message, ErrorKind.Unauthenticated);

    public static ErrorDefinition Forbidden(string code, string message) =>
        new(code, message, ErrorKind.Forbidden);

    public static ErrorDefinition PaymentRequired(string code, string message) =>
        new(code, message, ErrorKind.PaymentRequired);

    public static ValidationErrorDefinition Validation(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]> errors) =>
        new(code, message, errors);

    private string code = ValidateCode(Code);
    private string message = ValidateMessage(Message);
    private ErrorKind kind = ValidateKind(Kind);

    public string Code
    {
        get => code;
        init => code = ValidateCode(value);
    }

    public string Message
    {
        get => message;
        init => message = ValidateMessage(value);
    }

    public ErrorKind Kind
    {
        get => kind;
        init => kind = ValidateKind(value);
    }

    private static string ValidateCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (!ErrorCodePattern().IsMatch(code))
            throw new ArgumentException(
                "Error codes must contain at least two lowercase dot-separated segments.",
                nameof(code));

        return code;
    }

    private static string ValidateMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return message;
    }

    private static ErrorKind ValidateKind(ErrorKind kind)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown error kind.");

        return kind;
    }

    [GeneratedRegex(
        @"^[a-z][a-z0-9_]*(?:\.[a-z][a-z0-9_]*)+$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ErrorCodePattern();
}

public sealed record ValidationErrorDefinition(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]> Errors)
    : ErrorDefinition(Code, Message, ErrorKind.Invalid)
{
    private IReadOnlyDictionary<string, string[]> errors = ValidateErrors(Errors);

    public IReadOnlyDictionary<string, string[]> Errors
    {
        get => errors;
        init => errors = ValidateErrors(value);
    }

    private static IReadOnlyDictionary<string, string[]> ValidateErrors(
        IReadOnlyDictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0
            || errors.Any(error => error.Value is null
                || error.Value.Length == 0
                || error.Value.Any(string.IsNullOrWhiteSpace)))
        {
            throw new ArgumentException(
                "Validation errors must contain at least one non-empty message.",
                nameof(errors));
        }

        return errors;
    }
}

public interface IError
{
    ErrorDefinition Definition { get; }
    ErrorKind Kind { get; }
}
