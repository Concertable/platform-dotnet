namespace Concertable.Seed.Identity;

public enum ManagerKind { Venue, Artist }

/// <summary>A seed operator: their user identity and the tenant it owns, bound together so the credential,
/// the tenant entity, and the tenant announcement all derive from one source and can't drift apart.</summary>
public sealed record SeedManager(Guid Id, string Email, ManagerKind Kind)
{
    public Guid TenantId => TenantSeedIds.For(Id);
}

public static class SeedUsers
{
    public const int ManagerCount = 35;
    public const int TotalCount = 1 + ManagerCount * 2;

    public static Guid ArtistManagerId(int n) => new($"a1000000-0000-0000-0000-{n:D12}");
    public static string ArtistManagerEmail(int n) => $"artistmanager{n}@test.com";

    public static Guid VenueManagerId(int n) => new($"b1000000-0000-0000-0000-{n:D12}");
    public static string VenueManagerEmail(int n) => $"venuemanager{n}@test.com";

    public static readonly Guid Admin = new("a0000000-0000-0000-0000-000000000001");
    public const string AdminEmail = "admin@test.com";

    /// <summary>Every seed operator (venue + artist managers). The single source the Auth credential seed,
    /// B2B's tenant seed, and the seeding simulator all iterate — add a manager here and all three follow.</summary>
    public static IEnumerable<SeedManager> Managers
    {
        get
        {
            for (int i = 1; i <= ManagerCount; i++)
            {
                yield return new SeedManager(VenueManagerId(i), VenueManagerEmail(i), ManagerKind.Venue);
                yield return new SeedManager(ArtistManagerId(i), ArtistManagerEmail(i), ManagerKind.Artist);
            }
        }
    }
}
