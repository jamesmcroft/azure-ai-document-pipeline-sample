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

        // This service enables document data extraction using Azure AI Document Intelligence pre-built layout Markdown conversion combined with Azure OpenAI LLM prompt-based extraction.
        services.AddOpenAIMarkdownDocumentDataExtractor(options =>
        {
            // With this approach, you can use any GPT-3.5 or later model for extraction. Experiment with different models to find the most effective for your scenario.

            // options.DeploymentName = builder.Configuration[OpenAISettings.CompletionModelDeploymentConfigKey];
            options.DeploymentName = builder.Configuration[OpenAISettings.VisionCompletionModelDeploymentConfigKey];
        }, builder.Configuration);

        // This service enables document data extraction using only Azure OpenAI GPT-4 with Vision prompt-based extraction.
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
