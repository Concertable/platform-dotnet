using QuestPDF.Infrastructure;

namespace Concertable.Shared.Pdf.Application;

public interface IPdfRenderer
{
    byte[] Render(IDocument document);
}
