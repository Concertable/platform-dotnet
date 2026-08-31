using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Concertable.AppHost.Shared.UnitTests;

public sealed class ContainerImageResourceTests
{
    [Fact]
    public void AddContainerImage_DigestReference_ConfiguresServiceDiscoveryResourceWithDigest()
    {
        var builder = DistributedApplication.CreateBuilder();
        const string sha256 = "8b7ba47efb319e6e1f1b5b86223d4075b9c8e09920933dae24fbf35f72851a63";

        IResourceBuilder<IResourceWithServiceDiscovery> resource =
            builder.AddContainerImage("service", "ghcr.io/concertable/service", $"sha256:{sha256}");

        var service = Assert.IsType<ServiceContainerResource>(resource.Resource);
        var image = Assert.Single(service.Annotations.OfType<ContainerImageAnnotation>());

        Assert.Equal(sha256, image.SHA256);
    }

    [Fact]
    public void ImageOverloads_PublishedReturnTypes_SupportServiceDiscovery()
    {
        var methods = new[]
        {
            ImageOverload(typeof(Concertable.Auth.Hosting.AppHostExtensions), "AddAuth"),
            ImageOverload(typeof(Concertable.B2B.Hosting.AppHostExtensions), "AddB2BWeb"),
            ImageOverload(typeof(Concertable.B2B.Hosting.AppHostExtensions), "AddB2BWorkers"),
            ImageOverload(typeof(Concertable.B2B.Hosting.AppHostExtensions), "AddB2BSeedingSimulator"),
            ImageOverload(typeof(Concertable.Customer.Hosting.AppHostExtensions), "AddCustomerWeb"),
            ImageOverload(typeof(Concertable.Payment.Hosting.AppHostExtensions), "AddPaymentWeb"),
            ImageOverload(typeof(Concertable.Payment.Hosting.AppHostExtensions), "AddPaymentWorkers")
        };

        foreach (var method in methods)
        {
            var resourceType = Assert.Single(method.ReturnType.GetGenericArguments());

            Assert.True(typeof(IResourceWithServiceDiscovery).IsAssignableFrom(resourceType));
        }
    }

    private static System.Reflection.MethodInfo ImageOverload(Type extensions, string methodName) =>
        Assert.Single(
            extensions.GetMethods()
                .Where(method => method.Name == methodName)
                .Where(method => method.GetParameters().Any(parameter => parameter.Name == "digest")));
}
