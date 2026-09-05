public sealed record SpaSurface(
    string ResourceName,
    int HttpsPort)
{
    public string Origin => $"https://localhost:{HttpsPort}";
}
