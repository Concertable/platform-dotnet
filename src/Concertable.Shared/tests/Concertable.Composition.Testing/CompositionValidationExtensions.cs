using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Concertable.Composition.Testing;

public static class CompositionValidationExtensions
{
    public static void ValidateComposition(
        this IServiceCollection descriptors,
        IServiceProvider services,
        CompositionValidationOptions options)
    {
        var errors = new List<Exception>();
        var assemblies = LoadApplicationAssemblies(options.RootAssemblies);
        var activationTypes = GetActivationTypes(services, assemblies, options.IsFunction).ToArray();

        ValidateDescriptors(descriptors, services, assemblies, errors);
        ValidateOpenGenericConsumers(descriptors, services, activationTypes, options.HandlerServiceDefinitions, errors);
        ValidateActivationTypes(services, activationTypes, errors);
        ValidateHostedServices(services, errors);

        if (errors.Count > 0)
            throw new AggregateException("Composition validation failed.", errors);
    }

    private static IReadOnlySet<Assembly> LoadApplicationAssemblies(IEnumerable<Assembly> roots)
    {
        var assemblies = new HashSet<Assembly>();
        var pending = new Queue<Assembly>(roots);

        while (pending.TryDequeue(out var assembly))
        {
            if (!assemblies.Add(assembly))
                continue;

            foreach (var reference in assembly.GetReferencedAssemblies()
                         .Where(reference => reference.Name?.StartsWith("Concertable.", StringComparison.Ordinal) == true))
            {
                try
                {
                    pending.Enqueue(Assembly.Load(reference));
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Could not load application assembly {reference.FullName}.", exception);
                }
            }
        }

        return assemblies;
    }

    private static IEnumerable<Type> GetActivationTypes(
        IServiceProvider services,
        IReadOnlySet<Assembly> assemblies,
        Func<MethodInfo, bool>? isFunction)
    {
        var types = assemblies.SelectMany(GetLoadableTypes).Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition).ToArray();
        var controllerFeature = new ControllerFeature();
        services.GetService<ApplicationPartManager>()?.PopulateFeature(controllerFeature);

        return controllerFeature.Controllers.Select(type => type.AsType())
            .Concat(types.Where(type => typeof(PageModel).IsAssignableFrom(type)))
            .Concat(types.Where(type => type.Name.EndsWith("Middleware", StringComparison.Ordinal)))
            .Concat(isFunction is null
                ? []
                : types.Where(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Any(isFunction)))
            .Distinct();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    private static void ValidateDescriptors(
        IEnumerable<ServiceDescriptor> descriptors,
        IServiceProvider services,
        IReadOnlySet<Assembly> assemblies,
        ICollection<Exception> errors)
    {
        foreach (var group in descriptors
                     .Where(descriptor => !descriptor.ServiceType.IsGenericTypeDefinition)
                     .Where(descriptor => IsApplicationDescriptor(descriptor, assemblies))
                     .GroupBy(descriptor => (descriptor.ServiceType, descriptor.ServiceKey)))
        {
            TryValidate(
                $"registered service {group.Key.ServiceType.FullName}",
                provider => group.Key.ServiceKey is null
                    ? provider.GetServices(group.Key.ServiceType).ToArray()
                    : provider.GetKeyedServices(group.Key.ServiceType, group.Key.ServiceKey).ToArray(),
                services,
                errors);
        }
    }

    private static bool IsApplicationDescriptor(ServiceDescriptor descriptor, IReadOnlySet<Assembly> assemblies)
    {
        var implementationType = descriptor.IsKeyedService
            ? descriptor.KeyedImplementationType
            : descriptor.ImplementationType;
        var implementationInstance = descriptor.IsKeyedService
            ? descriptor.KeyedImplementationInstance
            : descriptor.ImplementationInstance;
        var factoryDeclaringType = descriptor.IsKeyedService
            ? descriptor.KeyedImplementationFactory?.Method.DeclaringType
            : descriptor.ImplementationFactory?.Method.DeclaringType;
        var factoryTargetType = descriptor.IsKeyedService
            ? descriptor.KeyedImplementationFactory?.Target?.GetType()
            : descriptor.ImplementationFactory?.Target?.GetType();
        return assemblies.Contains(descriptor.ServiceType.Assembly) ||
               implementationType is not null && assemblies.Contains(implementationType.Assembly) ||
               implementationInstance is not null && assemblies.Contains(implementationInstance.GetType().Assembly) ||
               factoryDeclaringType is not null && assemblies.Contains(factoryDeclaringType.Assembly) ||
               factoryTargetType is not null && assemblies.Contains(factoryTargetType.Assembly);
    }

    private static void ValidateOpenGenericConsumers(
        IEnumerable<ServiceDescriptor> descriptors,
        IServiceProvider services,
        IEnumerable<Type> activationTypes,
        IReadOnlyCollection<Type> handlerDefinitions,
        ICollection<Exception> errors)
    {
        var openRegistrations = descriptors
            .Where(descriptor => descriptor.ServiceType.IsGenericTypeDefinition)
            .Select(descriptor => descriptor.ServiceType)
            .ToHashSet();
        var implementationTypes = descriptors.SelectMany(GetImplementationTypes).Concat(activationTypes).Distinct();
        var closedServices = implementationTypes
            .SelectMany(type => type.GetConstructors().SelectMany(constructor => constructor.GetParameters()))
            .SelectMany(parameter => FlattenServiceTypes(parameter.ParameterType))
            .Where(type => type.IsConstructedGenericType)
            .Where(type => openRegistrations.Contains(type.GetGenericTypeDefinition()) || handlerDefinitions.Contains(type.GetGenericTypeDefinition()))
            .Distinct();

        foreach (var serviceType in closedServices)
            TryValidate($"closed generic service {serviceType.FullName}", provider => provider.GetServices(serviceType).ToArray(), services, errors);
    }

    private static IEnumerable<Type> GetImplementationTypes(ServiceDescriptor descriptor)
    {
        var implementationType = descriptor.IsKeyedService
            ? descriptor.KeyedImplementationType
            : descriptor.ImplementationType;
        if (implementationType is not null && !implementationType.IsGenericTypeDefinition)
            yield return implementationType;
    }

    private static IEnumerable<Type> FlattenServiceTypes(Type type)
    {
        yield return type;
        if (!type.IsConstructedGenericType)
            yield break;

        foreach (var argument in type.GetGenericArguments())
        foreach (var nested in FlattenServiceTypes(argument))
            yield return nested;
    }

    private static void ValidateActivationTypes(
        IServiceProvider services,
        IEnumerable<Type> activationTypes,
        ICollection<Exception> errors)
    {
        foreach (var type in activationTypes)
        {
            TryValidate(
                $"framework activation root {type.FullName}",
                provider => typeof(IMiddleware).IsAssignableFrom(type)
                    ? ActivatorUtilities.GetServiceOrCreateInstance(provider, type)
                    : type.Name.EndsWith("Middleware", StringComparison.Ordinal)
                        ? ActivatorUtilities.CreateInstance(provider, type, (RequestDelegate)(_ => Task.CompletedTask))
                        : ActivatorUtilities.CreateInstance(provider, type),
                services,
                errors);
        }
    }

    private static void ValidateHostedServices(IServiceProvider services, ICollection<Exception> errors) =>
        TryValidate("hosted services", provider => provider.GetServices<IHostedService>().ToArray(), services, errors);

    private static void TryValidate(
        string root,
        Func<IServiceProvider, object?> resolve,
        IServiceProvider services,
        ICollection<Exception> errors)
    {
        try
        {
            using var scope = services.CreateScope();
            resolve(scope.ServiceProvider);
        }
        catch (Exception exception)
        {
            errors.Add(new InvalidOperationException($"Failed to activate {root}.", exception));
        }
    }
}
