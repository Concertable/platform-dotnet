using System.Text.Json.Serialization;
using Concertable.Kernel;
using Concertable.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http;

namespace Concertable.Shared.Api.Http;

public sealed record ActionLink
{
    // Deserialization reaches this constructor too, and a member absent from the payload arrives as
    // its default rather than being rejected, so the guards are the only thing standing between an
    // incomplete payload and an ActionLink whose Href throws on first read.
    [JsonConstructor]
    private ActionLink(Href href, string method)
    {
        DomainException.ThrowIfNull(href, nameof(href));
        DomainException.ThrowIfNullOrWhiteSpace(method, nameof(method));

        this.Href = href;
        this.Method = method;
    }

    public Href Href { get; }

    public string Method { get; }

    public static ActionLink Get(string href) => new(Href.From(href), HttpMethods.Get);

    public static ActionLink Post(string href) => new(Href.From(href), HttpMethods.Post);
}
