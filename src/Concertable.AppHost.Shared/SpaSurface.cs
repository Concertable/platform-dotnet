public sealed record SpaSurface(
    string ResourceName,
    int HttpsPort,
    string? AuthClient)
{
    public string Origin => $"https://localhost:{HttpsPort}";
}
