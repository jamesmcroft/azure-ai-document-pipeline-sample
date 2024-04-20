using Microsoft.Extensions.Configuration;

namespace AIDocumentPipeline.Invoices;

public class InvoicesSettings(string invoicesStorageAccountName)
{
    public const string InvoicesStorageAccountConfigKey = "INVOICES_STORAGE_ACCOUNT_NAME";

    public const string InvoicesQueueConnectionConfigKey = "INVOICES_QUEUE_CONNECTION";

    public string InvoicesStorageAccountName { get; init; } = invoicesStorageAccountName;

    public static InvoicesSettings FromConfiguration(IConfiguration configuration)
    {
        var configInvoicesStorageAccountName = configuration[InvoicesStorageAccountConfigKey] ??
                                               throw new InvalidOperationException(
                                                   $"{InvoicesStorageAccountConfigKey} is not configured.");

        return new InvoicesSettings(configInvoicesStorageAccountName);
    }
}
