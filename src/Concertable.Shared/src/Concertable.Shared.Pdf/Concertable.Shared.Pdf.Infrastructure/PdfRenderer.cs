using Concertable.Shared.Pdf.Application;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Concertable.Shared.Pdf.Infrastructure;

internal sealed class PdfRenderer : IPdfRenderer
{
    // QuestPDF's GeneratePdf is not thread-safe: concurrent renders race on shared font-subset state
    // and emit PDFs with an unusable glyph map (text renders but can't be extracted). Serialize them.
    private static readonly Lock renderGate = new();

    public byte[] Render(IDocument document)
    {
        lock (renderGate)
            return document.GeneratePdf();
    }
}
