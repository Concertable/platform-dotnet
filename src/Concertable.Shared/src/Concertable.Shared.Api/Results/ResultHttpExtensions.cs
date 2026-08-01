using Concertable.Kernel.Errors;
using Concertable.Kernel.Functional;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Shared.Api.Results;

public static class ResultHttpExtensions
{
    public static ActionResult<TValue> ToActionResult<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, ActionResult<TValue>> onSuccess)
        where TValue : notnull
        where TError : IError =>
        result.Match<ActionResult<TValue>>(
            onSuccess,
            error => error.ToProblemActionResult());

    public static IActionResult ToActionResult<TError>(
        this UnitResult<TError> result,
        Func<IActionResult> onSuccess)
        where TError : IError =>
        result.Match<IActionResult>(
            onSuccess,
            error => error.ToProblemActionResult());

    public static ActionResult<TValue> ToOkActionResult<TValue, TError>(
        this Result<TValue, TError> result)
        where TValue : notnull
        where TError : IError =>
        result.ToActionResult(
            value => new OkObjectResult(value));

    public static ActionResult<TValue> ToCreatedAtActionResult<TValue, TError>(
        this Result<TValue, TError> result,
        string actionName,
        object? routeValues = null)
        where TValue : notnull
        where TError : IError =>
        result.ToActionResult(
            value => new CreatedAtActionResult(
                actionName,
                controllerName: null,
                routeValues,
                value));

    public static IActionResult ToNoContentActionResult<TError>(
        this UnitResult<TError> result)
        where TError : IError =>
        result.ToActionResult(
            () => new NoContentResult());
}
