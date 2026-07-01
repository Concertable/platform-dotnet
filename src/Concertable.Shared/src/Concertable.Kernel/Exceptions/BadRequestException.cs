using Concertable.Kernel.Enums;
using FluentResults;
using System.Net;

namespace Concertable.Kernel.Exceptions;

public sealed class BadRequestException : HttpException
{
    public IReadOnlyList<string>? ValidationErrors { get; }

    public BadRequestException(IReadOnlyList<string> errors)
        : base("One or more validation errors occurred.", HttpStatusCode.BadRequest)
    {
        Title = "Bad Request";
        ValidationErrors = errors;
    }

    public BadRequestException(IEnumerable<IError> errors)
        : base("One or more validation errors occurred.", HttpStatusCode.BadRequest)
    {
        Title = "Bad Request";
        ValidationErrors = errors.Select(e => e.Message).ToList();
    }

    public BadRequestException(string detail)
        : base(detail, HttpStatusCode.BadRequest)
    {
        Title = "Bad Request";
    }

    public BadRequestException(string detail, ErrorType errorType)
        : base(detail, errorType, HttpStatusCode.BadRequest)
    {
        Title = "Bad Request";
    }
}
