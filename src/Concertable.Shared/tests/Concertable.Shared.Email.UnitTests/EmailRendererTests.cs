using Concertable.Shared.Email.Infrastructure;

namespace Concertable.Shared.Email.UnitTests;

public sealed class EmailRendererTests
{
    private readonly MjmlEmailRenderer renderer = new();

    private sealed record Party(string LegalName, string? Vat);
    private sealed record Model(Party Venue, Party Artist);

    private const string Template =
        """
        <mjml><mj-body><mj-section><mj-column>
        <mj-text>{{ Venue.LegalName }}{{ if Venue.Vat }} — VAT {{ Venue.Vat }}{{ end }}</mj-text>
        <mj-text>{{ Artist.LegalName }}</mj-text>
        </mj-column></mj-section></mj-body></mjml>
        """;

    [Fact]
    public void Render_BindsTypedModel_AndCompilesMjmlToHtml()
    {
        var html = renderer.Render(Template, new Model(
            Venue: new Party("Roundhouse Trust Ltd", "GB123"),
            Artist: new Party("Aretha Live Ltd", null)));

        Assert.Contains("Roundhouse Trust Ltd — VAT GB123", html);
        Assert.Contains("Aretha Live Ltd", html);
        // MJML compiled to Outlook-safe HTML, model fully bound.
        Assert.Contains("<table", html);
        Assert.DoesNotContain("<mjml", html);
        Assert.DoesNotContain("{{", html);
    }

    [Fact]
    public void Render_OmitsConditionalBranch_WhenModelValueFalsy()
    {
        var html = renderer.Render(Template, new Model(
            Venue: new Party("Roundhouse Trust Ltd", Vat: null),
            Artist: new Party("Aretha Live Ltd", null)));

        Assert.DoesNotContain("VAT", html);
        Assert.Contains("Roundhouse Trust Ltd", html);
    }
}
