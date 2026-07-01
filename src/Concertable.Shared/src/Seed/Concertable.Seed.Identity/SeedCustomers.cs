namespace Concertable.Seed.Identity;

public static class SeedCustomers
{
    public const int CustomerCount = 3;

    public static Guid CustomerId(int n) => new($"c0000000-0000-0000-0000-{n:D12}");
    public static string CustomerEmail(int n) => $"customer{n}@test.com";
}
