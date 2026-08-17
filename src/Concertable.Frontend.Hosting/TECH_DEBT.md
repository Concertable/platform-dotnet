# Concertable.Frontend.Hosting technical debt

## MED

### `AddXSpa` methods hardcode port and surface-name literals

`AppHostExtensions.cs`'s `AddCustomerSpa`/`AddVenueSpa`/`AddArtistSpa`/`AddBusinessSpa`/`AddAdminSpa`
each pass their local dev HTTPS port (5174-5178) and surface directory name ("customer"/"venue"/
"artist"/"business"/"admin") as inline literals to `AddSpaSurface`, with no shared registry — a
collision or gap has to be caught by eye across five call sites.

**Resolves when:** the port + surface-name pairs move to one named table (e.g. a small
`SpaSurface` enum/record list) that `AddSpaSurface` iterates or looks up, so adding a sixth SPA can't
silently collide with an existing port. Low priority: this is local Aspire orchestration only (never
ships to prod), and the whole port-assignment scheme is likely to be reworked once a real production
deployment target replaces `dotnet run`-based local dev hosting.
