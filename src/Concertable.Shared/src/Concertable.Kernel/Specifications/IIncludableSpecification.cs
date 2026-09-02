namespace Concertable.Kernel.Specifications;

public interface IIncludableSpecification<TEntity, out TProperty> : ISpecificationBuilder<TEntity>
    where TEntity : class
{
    IncludePath<TEntity> Path { get; }
}
