using System.Linq.Expressions;

namespace Concertable.Kernel.Expressions;

internal sealed class ParameterReplacer(ParameterExpression target, Expression replacement)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
        => node == target ? replacement : base.VisitParameter(node);
}
