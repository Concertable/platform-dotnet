using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.Testing.Unit;

/// <summary>Creates a fresh, isolated EF Core InMemory database identity for a unit test.</summary>
public static class InMemoryDatabaseFactory
{
    public static (InMemoryDatabaseRoot Root, string Name) Create() =>
        (new InMemoryDatabaseRoot(), Guid.NewGuid().ToString());
}
