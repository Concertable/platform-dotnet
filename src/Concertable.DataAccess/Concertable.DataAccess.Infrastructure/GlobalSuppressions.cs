using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "MA0053:Make class or record sealed", Scope = "type", Target = "~T:Concertable.DataAccess.Infrastructure.UnitOfWork`1", Justification = "Generic base subclassed by per-module UnitOfWork types in other service assemblies, which the analyzer cannot see.")]
[assembly: SuppressMessage("Design", "MA0053:Make class or record sealed", Scope = "type", Target = "~T:Concertable.DataAccess.Infrastructure.UnitOfWorkBehavior`1", Justification = "Generic base subclassed by per-module UnitOfWorkBehavior types in other service assemblies, which the analyzer cannot see.")]
