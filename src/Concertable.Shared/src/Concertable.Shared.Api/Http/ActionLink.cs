using Concertable.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http;

namespace Concertable.Shared.Api.Http;

public sealed record ActionLink
{
    private ActionLink(Href href, string method)
    {
        this.Href = href;
        this.Method = method;
    }

    public Href Href { get; }

    public string Method { get; }

    public static ActionLink Get(string href) => new(Href.From(href), HttpMethods.Get);

    public static ActionLink Post(string href) => new(Href.From(href), HttpMethods.Post);

    public static ActionLink Put(string href) => new(Href.From(href), HttpMethods.Put);

    public static ActionLink Delete(string href) => new(Href.From(href), HttpMethods.Delete);
}
