using Concertable.Shared.Email.Infrastructure;

namespace Concertable.Shared.Email.UnitTests;

public sealed class EmailRendererTests
{
    private const string Template =
        """
        <mjml><mj-body><mj-section><mj-column>
        <mj-text>Booking confirmed: {{ Artist.Name }} at {{ Venue.Name }}</mj-text>
        <mj-text>{{ Venue.LegalName }}</mj-text>
        </mj-column></mj-section></mj-body></mjml>
        """;

    private readonly MjmlEmailRenderer renderer = new();

    [Fact]
    public void Render_BindsModelAndCompilesMjmlToHtml()
    {
        var html = renderer.Render(Template, new
        {
            Artist = new { Name = "Aretha" },
            Venue = new { Name = "The Roundhouse", LegalName = "Roundhouse Trust Ltd" },
        });

        Assert.Contains("Booking confirmed: Aretha at The Roundhouse", html);
        Assert.Contains("Roundhouse Trust Ltd", html);
        // MJML compiled to Outlook-safe HTML, not passed through raw.
        Assert.Contains("<table", html);
        Assert.DoesNotContain("<mjml", html);
        Assert.DoesNotContain("{{", html);
    }

    [Fact]
    public void Render_OmitsSectionForFalsyModelBranch()
    {
        var template =
            """
            <mjml><mj-body><mj-section><mj-column>
            {{ if Vat }}<mj-text>VAT number: {{ Vat }}</mj-text>{{ end }}
            <mj-text>{{ LegalName }}</mj-text>
            </mj-column></mj-section></mj-body></mjml>
            """;

        var withVat = renderer.Render(template, new { Vat = "GB123", LegalName = "Aretha Live Ltd" });
        var withoutVat = renderer.Render(template, new { Vat = (string?)null, LegalName = "Aretha Live Ltd" });

        Assert.Contains("VAT number: GB123", withVat);
        Assert.DoesNotContain("VAT number", withoutVat);
        Assert.Contains("Aretha Live Ltd", withoutVat);
    }
}
