namespace Concertable.Shared.Pdf.Application;

// Temporary alias for IPdfRenderer while consumers migrate off IPdfService; removed once nothing injects it.
public interface IPdfService : IPdfRenderer;
