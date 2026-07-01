using Concertable.Shared.Pdf.Application;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace Concertable.Shared.Pdf.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedPdf(this IServiceCollection services)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        services.AddScoped<IPdfService, PdfService>();
        return services;
    }
}
