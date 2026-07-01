namespace Concertable.Seed.Shared.Identity;

public sealed class SeedingScope
{
    private int depth;

    public bool IsActive => depth > 0;

    public IDisposable Activate()
    {
        depth++;
        return new Activation(this);
    }

    private sealed class Activation : IDisposable
    {
        private SeedingScope? scope;

        public Activation(SeedingScope scope)
        {
            this.scope = scope;
        }

        public void Dispose()
        {
            if (scope is null)
                return;

            scope.depth--;
            scope = null;
        }
    }
}
