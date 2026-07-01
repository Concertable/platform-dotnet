using Concertable.Shared.Pdf.Application;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Concertable.Shared.Pdf.Infrastructure;

internal sealed class PdfService : IPdfService
{
    public byte[] Render(IDocument document) => document.GeneratePdf();
}
