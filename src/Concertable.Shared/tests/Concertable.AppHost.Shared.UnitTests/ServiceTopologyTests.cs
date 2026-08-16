using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Concertable.Auth.Contracts.Events;
using Concertable.Auth.Hosting;
using Concertable.B2B.Artist.Contracts.Events;
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
            .Subscribe<ConcertPostedEvent>("consumer")
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
            typeof(B2BPayoutOwnerRegisteredEvent),
            typeof(TenantActivityRecordedEvent));

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
