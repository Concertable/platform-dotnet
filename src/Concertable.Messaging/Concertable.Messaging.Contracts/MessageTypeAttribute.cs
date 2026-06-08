using System.Reflection;

namespace Concertable.Messaging.Contracts;

[AttributeUsage(AttributeTargets.Class)]
public sealed class MessageTypeAttribute(string name) : Attribute
{
    public string Name { get; } = name;

    public static string Resolve(Type type) =>
        type.GetCustomAttribute<MessageTypeAttribute>()?.Name
        ?? throw new InvalidOperationException(
            $"'{type.Name}' is missing [MessageType(\"...\")].");
}
