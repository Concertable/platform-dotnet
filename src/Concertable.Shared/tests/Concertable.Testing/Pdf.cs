using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Concertable.Testing;

public static class Pdf
{
    public static string ExtractText(byte[] bytes)
    {
        using var pdf = PdfDocument.Open(bytes);
        var raw = string.Join(" ", pdf.GetPages().Select(p => ContentOrderTextExtractor.GetText(p)));
        return Regex.Replace(raw, @"\s+", " ");
    }
}
