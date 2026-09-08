using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Concertable.Frontend.Hosting;

public sealed record MobileServiceEndpoint(
    IResourceBuilder<IResourceWithServiceDiscovery> Service,
    string EnvironmentVariable);
