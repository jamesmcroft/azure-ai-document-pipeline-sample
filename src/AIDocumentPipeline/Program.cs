using AIDocumentPipeline.Shared.Documents;
using AIDocumentPipeline.Shared.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((hostContext, configuration) =>
    {
        configuration.SetBasePath(hostContext.HostingEnvironment.ContentRootPath)
                     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();
    })
    .ConfigureServices((builder, services) =>
    {
        services.AddObservability(
            builder.Configuration,
            builder.HostingEnvironment.ApplicationName,
            builder.HostingEnvironment.IsDevelopment());

        services.AddDocumentIntelligenceMarkdownConverter(builder.Configuration);
        services.AddOpenAIDocumentDataExtractor(_ => { }, builder.Configuration);
    })
    .Build();

host.Run();

namespace AIDocumentPipeline
{
    /// <summary>
    /// Defines the entry point for the application.
    /// </summary>
    public partial class Program;
}
