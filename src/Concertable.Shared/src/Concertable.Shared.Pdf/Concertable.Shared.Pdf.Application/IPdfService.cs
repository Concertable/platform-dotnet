using QuestPDF.Infrastructure;

namespace Concertable.Shared.Pdf.Application;

public interface IPdfService
{
    byte[] Render(IDocument document);
}
