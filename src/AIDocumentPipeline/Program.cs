using AIDocumentPipeline.Invoices;
using AIDocumentPipeline.Shared.Documents;
using AIDocumentPipeline.Shared.Documents.OpenAI;
using AIDocumentPipeline.Shared.Observability;
using AIDocumentPipeline.Shared.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        services.AddAzureBlobStorage(builder.Configuration);

        // Adds the Azure OpenAI document data extractor using vision capabilities of GPT-4o or GPT-4o-mini models.
        services.AddOpenAIVisionDocumentDataExtractor(options =>
        {
            options.DeploymentName = builder.Configuration[OpenAISettings.VisionCompletionModelDeploymentConfigKey];
        }, builder.Configuration);

        services.TryAddSingleton(_ => InvoicesSettings.FromConfiguration(builder.Configuration));
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
