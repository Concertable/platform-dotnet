using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public sealed class IncludePath<TEntity> where TEntity : class
{
    private readonly List<LambdaExpression> steps;

    internal IncludePath(LambdaExpression root)
    {
        this.steps = [root];
    }

    public IReadOnlyList<LambdaExpression> Steps => this.steps.AsReadOnly();

    internal void Append(LambdaExpression step) => this.steps.Add(step);
}
