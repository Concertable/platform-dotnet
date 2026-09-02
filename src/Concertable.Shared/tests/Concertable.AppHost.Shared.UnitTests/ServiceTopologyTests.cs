using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Concertable.Auth.Contracts.Events;
using Concertable.Auth.Hosting;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Commands;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Hosting;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Customer.Hosting;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Customer.Ticket.Contracts.Events;
using Concertable.Messaging.AzureServiceBus.Options;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Events;
using Concertable.Payment.Hosting;
using Concertable.Shared.Email.Application;
using B2BPayoutOwnerRegisteredEvent = Concertable.B2B.Tenant.Contracts.Events.PayoutOwnerRegisteredEvent;
using TenantActivityRecordedEvent = Concertable.B2B.Tenant.Contracts.Events.TenantActivityRecordedEvent;

namespace Concertable.AppHost.Shared.UnitTests;

public sealed class ServiceTopologyTests
{
    [Fact]
    public void PublishAndSubscribe_ProvisionOneTopic()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddAzureServiceBus("messaging")
            .Topology()
            .Publish<ConcertPostedEvent>()
            .WithService("consumer")
            .Subscribe<ConcertPostedEvent>()
            .RunAsEmulator();

        var topicName = new AzureServiceBusOptions().TopicNameFor(typeof(ConcertPostedEvent));
        var topics = builder.Resources
            .OfType<AzureServiceBusTopicResource>()
            .Where(resource => resource.Name == topicName);
        var subscription = Assert.Single(builder.Resources.OfType<AzureServiceBusSubscriptionResource>());

        Assert.Single(topics);
        Assert.Equal("consumer", subscription.SubscriptionName);
    }

    [Fact]
    public void WithService_PerService_ScopesEachSubscriptionToItsOwnServiceName()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddAzureServiceBus("messaging")
            .Topology()
            .Publish<ConcertPostedEvent>()
            .Publish<ConcertChangedEvent>()
            .WithService("service-a")
            .Subscribe<ConcertPostedEvent>()
            .WithService("service-b")
            .Subscribe<ConcertChangedEvent>()
            .RunAsEmulator();

        var subscriptionNames = builder.Resources
            .OfType<AzureServiceBusSubscriptionResource>()
            .Select(subscription => subscription.SubscriptionName)
            .ToHashSet();

        Assert.Equal(["service-a", "service-b"], subscriptionNames.Order());
    }

    [Fact]
    public void WithService_ScopedBuildersStayIndependentWhenInterleaved()
    {
        var builder = DistributedApplication.CreateBuilder();
        var topology = builder.AddAzureServiceBus("messaging").Topology();
        var serviceA = topology.WithService("service-a");
        var serviceB = topology.WithService("service-b");

        serviceA.Subscribe<ConcertPostedEvent>();
        serviceB.Subscribe<ConcertChangedEvent>();
        serviceA.Subscribe<ArtistChangedEvent>();

        var perService = builder.Resources
            .OfType<AzureServiceBusSubscriptionResource>()
            .GroupBy(subscription => subscription.SubscriptionName)
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.Equal(2, perService["service-a"]);
        Assert.Equal(1, perService["service-b"]);
    }

    [Fact]
    public void WithService_Publish_ProvisionsTheTopicWithoutScopingIt()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddAzureServiceBus("messaging")
            .Topology()
            .WithService("service-a")
            .Publish<ConcertPostedEvent>()
            .Subscribe<ConcertChangedEvent>();

        var topicName = new AzureServiceBusOptions().TopicNameFor(typeof(ConcertPostedEvent));
        var topics = builder.Resources.OfType<AzureServiceBusTopicResource>().Select(topic => topic.Name);
        var subscriptions = builder.Resources
            .OfType<AzureServiceBusSubscriptionResource>()
            .Select(subscription => subscription.SubscriptionName);

        Assert.Contains(topicName, topics);
        Assert.Equal(["service-a"], subscriptions);
    }

    [Fact]
    public void WithService_Queue_NamesTheQueueForThatService()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddAzureServiceBus("messaging")
            .Topology()
            .WithService("service-a")
            .Queue<SendEmailCommand>();

        var expected = new AzureServiceBusOptions().QueueNameFor("service-a", typeof(SendEmailCommand));
        var queues = builder.Resources.OfType<AzureServiceBusQueueResource>().Select(queue => queue.QueueName);

        Assert.Contains(expected, queues);
    }

    [Fact]
    public void PublishWithoutSubscriber_ProvisionsExpiringEmulatorSink()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddAzureServiceBus("messaging")
            .Topology()
            .Publish<ConcertPostedEvent>()
            .RunAsEmulator();

        var subscription = Assert.Single(builder.Resources.OfType<AzureServiceBusSubscriptionResource>());

        Assert.Equal("emulator-sink", subscription.SubscriptionName);
        Assert.Equal(TimeSpan.FromMinutes(1), subscription.DefaultMessageTimeToLive);
    }

    [Fact]
    public void AddAuthTopology_ProvisionsPublishedEventTopics() =>
        AssertPublishedTopics(
            topology => topology.AddAuthTopology(),
            typeof(CredentialRegisteredEvent));

    [Fact]
    public void AddB2BTopology_ProvisionsPublishedEventTopics() =>
        AssertPublishedTopics(
            topology => topology.AddB2BTopology(),
            typeof(ArtistChangedEvent),
            typeof(ArtistRatingUpdatedEvent),
            typeof(VenueChangedEvent),
            typeof(VenueRatingUpdatedEvent),
            typeof(ConcertChangedEvent),
            typeof(ConcertPostedEvent),
            typeof(ConcertRatingUpdatedEvent),
            typeof(BookingCancelledEvent),
            typeof(ConcertCancelledEvent),
            typeof(ConcertCreatedEvent),
            typeof(B2BPayoutOwnerRegisteredEvent),
            typeof(TenantActivityRecordedEvent));

    [Fact]
    public void AddB2BTopology_ProvisionsCommandQueues()
    {
        var builder = DistributedApplication.CreateBuilder();
        var topology = builder.AddAzureServiceBus("messaging").Topology();
        topology.AddB2BTopology();

        var queues = builder.Resources
            .OfType<AzureServiceBusQueueResource>()
            .Select(queue => queue.Name)
            .ToHashSet();
        var options = new AzureServiceBusOptions();

        Assert.Equal(2, queues.Count);
        Assert.Contains(options.QueueNameFor(B2BConstants.ServiceName, typeof(SendEmailCommand)), queues);
        Assert.Contains(options.QueueNameFor(B2BConstants.ServiceName, typeof(NotifyConcertDraftCreatedCommand)), queues);
    }

    [Fact]
    public void AddCustomerTopology_ProvisionsPublishedEventTopics() =>
        AssertPublishedTopics(
            topology => topology.AddCustomerTopology(),
            typeof(CustomerReviewSubmittedEvent),
            typeof(TicketPurchasedEvent));

    [Fact]
    public void AddPaymentTopology_ProvisionsPublishedEventTopics() =>
        AssertPublishedTopics(
            topology => topology.AddPaymentTopology(),
            typeof(PaymentSucceededEvent),
            typeof(PaymentFailedEvent),
            typeof(CaptureEscrowSucceededEvent),
            typeof(CaptureEscrowRejectedEvent),
            typeof(DepositEscrowSucceededEvent),
            typeof(DepositEscrowRejectedEvent),
            typeof(RefundEscrowSucceededEvent),
            typeof(RefundEscrowRejectedEvent),
            typeof(RefundEscrowDeferredEvent));

    private static void AssertPublishedTopics(Action<AsbTopology> configure, params Type[] eventTypes)
    {
        var builder = DistributedApplication.CreateBuilder();
        var topology = builder.AddAzureServiceBus("messaging").Topology();
        configure(topology);
        topology.RunAsEmulator();

        var topics = builder.Resources
            .OfType<AzureServiceBusTopicResource>()
            .Select(resource => resource.Name)
            .ToHashSet();
        var options = new AzureServiceBusOptions();

        foreach (var eventType in eventTypes)
            Assert.Contains(options.TopicNameFor(eventType), topics);
    }
}
