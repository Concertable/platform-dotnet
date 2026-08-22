public enum LocalSpaClient
{
    Customer,
    Venue,
    Artist,
    Admin
}

public sealed record LocalSpaSurface(
    string ResourceName,
    int HttpsPort,
    LocalSpaClient? AuthClient)
{
    public string Origin => $"https://localhost:{HttpsPort}";
}

public static class LocalSpaSurfaces
{
    public static LocalSpaSurface Customer { get; } = new("customer", 5174, LocalSpaClient.Customer);
    public static LocalSpaSurface Venue { get; } = new("venue", 5175, LocalSpaClient.Venue);
    public static LocalSpaSurface Artist { get; } = new("artist", 5176, LocalSpaClient.Artist);
    public static LocalSpaSurface Business { get; } = new("business", 5177, null);
    public static LocalSpaSurface Admin { get; } = new("admin", 5178, LocalSpaClient.Admin);

    public static IReadOnlyList<LocalSpaSurface> All { get; } =
        Array.AsReadOnly([Customer, Venue, Artist, Business, Admin]);

    public static IReadOnlyList<LocalSpaSurface> Authenticated { get; } =
        Array.AsReadOnly([Customer, Venue, Artist, Admin]);

    public static IReadOnlyList<LocalSpaSurface> B2B { get; } =
        Array.AsReadOnly([Venue, Artist, Business, Admin]);
}
