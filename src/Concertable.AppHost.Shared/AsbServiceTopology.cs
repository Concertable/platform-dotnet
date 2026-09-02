using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

public sealed class AsbServiceTopology
{
    private readonly AsbTopology topology;
    private readonly string serviceName;

    public AsbServiceTopology(AsbTopology topology, string serviceName)
    {
        this.topology = topology;
        this.serviceName = serviceName;
    }

    public AsbServiceTopology Subscribe<TEvent>()
    {
        this.topology.Subscribe<TEvent>(this.serviceName);
        return this;
    }

    public AsbServiceTopology Queue<TCommand>()
    {
        this.topology.Queue<TCommand>(this.serviceName);
        return this;
    }

    public AsbServiceTopology WithService(string otherServiceName) =>
        this.topology.WithService(otherServiceName);

    public IResourceBuilder<AzureServiceBusResource> RunAsEmulator() => this.topology.RunAsEmulator();
}
