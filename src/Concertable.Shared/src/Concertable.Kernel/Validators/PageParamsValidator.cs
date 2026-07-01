using Concertable.Contracts;
using FluentValidation;

namespace Concertable.Kernel.Validators;

public sealed class PageParamsValidator : AbstractValidator<IPageParams>
{
    public PageParamsValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
