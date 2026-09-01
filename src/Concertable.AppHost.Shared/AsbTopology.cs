using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
using Concertable.Messaging.AzureServiceBus.Options;
using System.Text.RegularExpressions;

public sealed class AsbTopology
{
    private const string EventSuffix = "Event";
    private readonly IResourceBuilder<AzureServiceBusResource> asb;
    private readonly AzureServiceBusOptions options = new();
    private readonly Dictionary<string, IResourceBuilder<AzureServiceBusTopicResource>> topics = new();
    private readonly HashSet<string> subscribedTopics = [];
    private string? serviceName;

    public AsbTopology(IResourceBuilder<AzureServiceBusResource> asb) => this.asb = asb;

    public AsbTopology Publish<TEvent>()
    {
        GetOrAddTopic<TEvent>();
        return this;
    }

    /// <summary>
    /// Scopes the subscriptions and queues <paramref name="configure"/> declares to
    /// <paramref name="serviceName"/>. Every service composes onto one builder, so the name is restored
    /// afterwards rather than left set: a name that outlived its block would let the next topology's
    /// <see cref="Subscribe{TEvent}"/> silently provision under the previous service.
    /// </summary>
    public AsbTopology ForService(string serviceName, Action<AsbTopology> configure)
    {
        var enclosing = this.serviceName;
        this.serviceName = serviceName;
        try
        {
            configure(this);
        }
        finally
        {
            serviceName = enclosing;
        }

        return this;
    }

    public AsbTopology Subscribe<TEvent>()
    {
        SubscribeCore<TEvent>(RequireServiceName());
        return this;
    }

    public AsbTopology Queue<TCommand>()
    {
        QueueCore<TCommand>(RequireServiceName());
        return this;
    }

    // The per-call service name every topology still passes. Superseded by ForService and removed once the
    // published package carrying it reaches them.
    public AsbTopology Subscribe<TEvent>(string serviceName)
    {
        SubscribeCore<TEvent>(serviceName);
        return this;
    }

    public AsbTopology Queue<TCommand>(string serviceName)
    {
        QueueCore<TCommand>(serviceName);
        return this;
    }

    public IResourceBuilder<AzureServiceBusResource> RunAsEmulator()
    {
        foreach (var (topic, topicBuilder) in topics)
        {
            if (subscribedTopics.Contains(topic))
                continue;

            topicBuilder
                .AddServiceBusSubscription($"{topic}-emulator-sink", "emulator-sink")
                .WithProperties(subscription => subscription.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1));
        }

        return asb.RunAsEmulator();
    }

    private string RequireServiceName() =>
        serviceName ?? throw new InvalidOperationException(
            $"Declare subscriptions and queues inside {nameof(ForService)}.");

    private void SubscribeCore<TEvent>(string forServiceName)
    {
        var topicBuilder = GetOrAddTopic<TEvent>();
        topicBuilder.AddServiceBusSubscription($"{forServiceName}-{KebabCase(typeof(TEvent))}", forServiceName);
        subscribedTopics.Add(topicBuilder.Resource.TopicName);
    }

    private void QueueCore<TCommand>(string forServiceName) =>
        asb.AddServiceBusQueue(options.QueueNameFor(forServiceName, typeof(TCommand)));

    private IResourceBuilder<AzureServiceBusTopicResource> GetOrAddTopic<TEvent>()
    {
        var topic = options.TopicNameFor(typeof(TEvent));
        if (!topics.TryGetValue(topic, out var topicBuilder))
            topics[topic] = topicBuilder = asb.AddServiceBusTopic(topic);

        return topicBuilder;
    }

    private static string KebabCase(Type eventType)
    {
        var name = eventType.Name.EndsWith(EventSuffix, StringComparison.Ordinal)
            ? eventType.Name[..^EventSuffix.Length]
            : eventType.Name;
        return Regex.Replace(name, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
    }
}
