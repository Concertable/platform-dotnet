namespace Concertable.Kernel.Specifications;

internal sealed class IncludableSpecification<TEntity, TProperty> : IIncludableSpecification<TEntity, TProperty>
    where TEntity : class
{
    public IncludableSpecification(IncludePath<TEntity> path)
    {
        this.Path = path;
    }

    public IncludePath<TEntity> Path { get; }
}
