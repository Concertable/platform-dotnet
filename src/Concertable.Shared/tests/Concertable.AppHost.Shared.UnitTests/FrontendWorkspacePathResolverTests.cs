using Concertable.Frontend.Hosting;

namespace Concertable.AppHost.Shared.UnitTests;

public sealed class FrontendWorkspacePathResolverTests
{
    private readonly string repoRoot;
    private readonly string appHostDirectory;
    private readonly string monorepoWorkspace;
    private readonly string ownerWorkspace;
    private readonly string[][] workspacePathCandidates;

    public FrontendWorkspacePathResolverTests()
    {
        this.repoRoot = Path.Combine(Path.GetTempPath(), "concertable-workspace-path-test");
        this.appHostDirectory = Path.Combine(this.repoRoot, "api", "Concertable.B2B", "src", "Concertable.B2B.AppHost");
        this.monorepoWorkspace = Path.Combine(this.repoRoot, "app", "web", "b2b", "venue");
        this.ownerWorkspace = Path.Combine(this.repoRoot, "app", "web", "venue");
        this.workspacePathCandidates =
        [
            ["app", "web", "b2b", "venue"],
            ["app", "web", "venue"]
        ];
    }

    [Fact]
    public void Resolve_BothLayoutsExist_PrefersMonorepoLayout()
    {
        var existing = new HashSet<string>([this.monorepoWorkspace, this.ownerWorkspace]);

        var result = FrontendWorkspacePathResolver.Resolve(
            this.appHostDirectory,
            this.workspacePathCandidates,
            existing.Contains);

        Assert.Equal(this.monorepoWorkspace, result);
    }

    [Fact]
    public void Resolve_OnlyOwnerLayoutExists_ReturnsOwnerLayout()
    {
        var existing = new HashSet<string>([this.ownerWorkspace]);

        var result = FrontendWorkspacePathResolver.Resolve(
            this.appHostDirectory,
            this.workspacePathCandidates,
            existing.Contains);

        Assert.Equal(this.ownerWorkspace, result);
    }

    [Fact]
    public void Resolve_NoLayoutExists_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            FrontendWorkspacePathResolver.Resolve(
                this.appHostDirectory,
                this.workspacePathCandidates,
                _ => false));

        Assert.Contains("app\\web\\b2b\\venue", exception.Message);
        Assert.Contains("app\\web\\venue", exception.Message);
    }
}
