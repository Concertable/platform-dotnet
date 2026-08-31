namespace Concertable.Kernel.Specifications;

public interface IIncludableSpecification<TEntity, out TProperty> where TEntity : class
{
    IncludePath<TEntity> Path { get; }
}
