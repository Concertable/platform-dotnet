using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

public sealed class ServiceContainerResource : ContainerResource, IResourceWithServiceDiscovery
{
    public ServiceContainerResource(string name)
        : base(name, null)
    {
    }
}
