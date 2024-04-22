using System.Globalization;
using AIDocumentPipeline.Shared;
using AIDocumentPipeline.Shared.Observability;
using AIDocumentPipeline.Shared.Validation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Invoices.Activities;

[ActivitySource]
public class ValidateInvoiceData()
    : BaseActivity(Name)
{
    public const string Name = nameof(ValidateInvoiceData);

    [Function(Name)]
    public Task<Result> RunAsync(
        [ActivityTrigger] Request input,
        FunctionContext context)
    {
        using var span = StartActiveSpan(Name, input);
        var logger = context.GetLogger(Name);

        var result = new Result { Name = input.InvoiceName ?? Name };

        var validationResult = input.Validate();
        if (!result.IsValid)
        {
            result.Merge(validationResult);
            return Task.FromResult(result);
        }

        var data = input.Data!;

        if (string.IsNullOrWhiteSpace(data.CustomerName))
        {
            result.Status |= ResultStatus.CustomerNameMissing;
            result.AddError(
                Name,
                $"{nameof(data.CustomerName)} is required.",
                logger,
                LogLevel.Error);
        }

        if (data.Signatures == null)
        {
            result.Status |= ResultStatus.DistributorSignatureMissing | ResultStatus.CustomerSignatureMissing;
            result.AddError(
                Name,
                $"{nameof(data.Signatures)} is required.",
                logger,
                LogLevel.Error);
        }

        if (data.Signatures != null)
        {
            var distributorSignature = GetSignature(data.Signatures, "Distributor");
            if (distributorSignature?.SignedOn == null ||
                distributorSignature.SignedOn.Value == DateTime.MinValue)
            {
                result.Status |= ResultStatus.DistributorSignatureMissing;
                result.AddError(
                    Name,
                    $"{nameof(data.Signatures)} must contain a distributor signature.",
                    logger,
                    LogLevel.Error);
            }

            var customerSignature = GetSignature(data.Signatures, "Customer");
            if (customerSignature?.SignedOn == null ||
                customerSignature.SignedOn.Value == DateTime.MinValue)
            {
                result.Status |= ResultStatus.CustomerSignatureMissing;
                result.AddError(
                    Name,
                    $"{nameof(data.Signatures)} must contain a customer signature.",
                    logger,
                    LogLevel.Error);
            }
        }

        if (data.Products != null)
        {
            if (!data.Products.Sum(x => x.Quantity).Equals(data.TotalQuantity))
            {
                result.Status |= ResultStatus.ProductsTotalQuantityInvalid;
                result.AddError(
                    Name,
                    $"{nameof(data.Products)} quantity total must match {nameof(data.TotalQuantity)}.",
                    logger,
                    LogLevel.Error);
            }

            if (!data.Products.Sum(x => x.Total).Equals(data.TotalPrice))
            {
                result.Status |= ResultStatus.ProductsTotalPriceInvalid;
                result.AddError(
                    Name,
                    $"{nameof(data.Products)} price total must match {nameof(data.TotalPrice)}.",
                    logger,
                    LogLevel.Error);
            }
        }

        if (result.IsValid)
        {
            result.Status = ResultStatus.Success;
        }

        return Task.FromResult(result);
    }

    private static InvoiceData.InvoiceDataSignature? GetSignature(
        IEnumerable<InvoiceData.InvoiceDataSignature> signatures,
        string type)
    {
        return signatures
            .FirstOrDefault(x =>
                x.Type != null && x.Type.Contains(
                    type,
                    CultureInfo.InvariantCulture,
                    CompareOptions.IgnoreCase));
    }

    public class Request : BaseWorkflowRequest
    {
        public string? InvoiceName { get; set; }

        public InvoiceData? Data { get; set; }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(InvoiceName))
            {
                result.AddError($"{nameof(InvoiceName)} is required.");
            }

            if (Data == null)
            {
                result.AddError($"{nameof(Data)} is required.");
            }

            return result;
        }
    }

    public class Result : WorkflowResult
    {
        public ResultStatus Status { get; set; }
    }

    [Flags]
    public enum ResultStatus
    {
        Unknown = 0,
        DistributorSignatureMissing = 1,
        CustomerSignatureMissing = 2,
        CustomerNameMissing = 3,
        ProductsTotalQuantityInvalid = 4,
        ProductsTotalPriceInvalid = 5,
        Success = 6
    }
}
