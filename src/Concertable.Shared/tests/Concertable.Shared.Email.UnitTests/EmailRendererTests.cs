using Concertable.Shared.Email.Application;
using Concertable.Shared.Email.Infrastructure;

namespace Concertable.Shared.Email.UnitTests;

public sealed class EmailRendererTests
{
    private readonly EmailRenderer renderer = new();

    private sealed record Party(string LegalName, string? Vat);

    private sealed class BookingEmail : IEmailContent
    {
        public required Party Venue { get; init; }
        public required Party Artist { get; init; }

        public string Subject => $"Booking confirmed with {Venue.LegalName}";

        public string Template =>
            """
            <mjml><mj-body><mj-section><mj-column>
            <mj-text>{{ Venue.LegalName }}{{ if Venue.Vat }} — VAT {{ Venue.Vat }}{{ end }}</mj-text>
            <mj-text>{{ Artist.LegalName }}</mj-text>
            </mj-column></mj-section></mj-body></mjml>
            """;
    }

    [Fact]
    public void Render_BindsContent_AndCompilesMjmlToHtml()
    {
        var result = renderer.Render(new BookingEmail
        {
            Venue = new Party("Roundhouse Trust Ltd", "GB123"),
            Artist = new Party("Aretha Live Ltd", null),
        });

        Assert.Equal("Booking confirmed with Roundhouse Trust Ltd", result.Subject);
        Assert.Contains("Roundhouse Trust Ltd — VAT GB123", result.HtmlBody);
        Assert.Contains("Aretha Live Ltd", result.HtmlBody);
        Assert.Contains("<table", result.HtmlBody);
        Assert.DoesNotContain("{{", result.HtmlBody);
    }

    [Fact]
    public void Render_OmitsConditional_WhenModelValueFalsy()
    {
        var result = renderer.Render(new BookingEmail
        {
            Venue = new Party("Roundhouse Trust Ltd", Vat: null),
            Artist = new Party("Aretha Live Ltd", null),
        });

        Assert.DoesNotContain("VAT", result.HtmlBody);
        Assert.Contains("Roundhouse Trust Ltd", result.HtmlBody);
    }
}
