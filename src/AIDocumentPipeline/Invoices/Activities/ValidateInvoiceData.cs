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

        ValidateInvoiceProducts(data, result, logger);
        ValidateInvoiceProductSignatures(data, result, logger);

        ValidateInvoiceReturns(data, result, logger);
        ValidateInvoiceReturnSignatures(data, result, logger);

        if (result.IsValid)
        {
            result.Status = ResultStatus.Success;
        }

        return Task.FromResult(result);
    }

    private static void ValidateInvoiceProducts(InvoiceData data, Result result, ILogger logger)
    {
        if (data.Products == null)
        {
            result.Status |= ResultStatus.ProductsMissing;
            result.AddError(
                Name,
                $"{nameof(data.Products)} is required.",
                logger,
                LogLevel.Error);
        }
        else
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
    }

    private static void ValidateInvoiceProductSignatures(InvoiceData data, Result result, ILogger logger)
    {
        if (data.ProductsSignatures == null)
        {
            result.Status |= ResultStatus.ProductsDriverSignatureMissing |
                             ResultStatus.ProductsCustomerSignatureMissing;
            result.AddError(
                Name,
                $"{nameof(data.ProductsSignatures)} is required.",
                logger,
                LogLevel.Error);
        }
        else
        {
            var driverSignature = GetSignature(data.ProductsSignatures, "Driver");
            if (driverSignature is null)
            {
                result.Status |= ResultStatus.ProductsDriverSignatureMissing;
                result.AddError(
                    Name,
                    $"{nameof(data.ProductsSignatures)} must contain a driver signature.",
                    logger,
                    LogLevel.Error);
            }

            var customerSignature = GetSignature(data.ProductsSignatures, "Customer");
            if (customerSignature is null)
            {
                result.Status |= ResultStatus.ProductsCustomerSignatureMissing;
                result.AddError(
                    Name,
                    $"{nameof(data.ProductsSignatures)} must contain a customer signature.",
                    logger,
                    LogLevel.Error);
            }
        }
    }

    private static void ValidateInvoiceReturns(InvoiceData data, Result result, ILogger logger)
    {
        if (data.Returns == null)
        {
            return;
        }

        foreach (var productReturn in data.Returns)
        {
            if (string.IsNullOrWhiteSpace(productReturn.Reason))
            {
                result.Status |= ResultStatus.ReturnReasonMissing;
                result.AddError(
                    Name,
                    $"{productReturn.Id} must contain a reason.",
                    logger,
                    LogLevel.Error);
            }
        }
    }

    private static void ValidateInvoiceReturnSignatures(InvoiceData data, Result result, ILogger logger)
    {
        if (data.ReturnsSignatures == null)
        {
            return;
        }

        var driverSignature = GetSignature(data.ReturnsSignatures, "Driver");
        if (driverSignature is null || string.IsNullOrWhiteSpace(driverSignature.Name))
        {
            result.Status |= ResultStatus.ReturnsDriverSignatureMissing;
            result.AddError(
                Name,
                $"{nameof(data.ReturnsSignatures)} must contain a driver signature.",
                logger,
                LogLevel.Error);
        }

        var customerSignature = GetSignature(data.ReturnsSignatures, "Customer");
        if (customerSignature is null || string.IsNullOrWhiteSpace(customerSignature.Name))
        {
            result.Status |= ResultStatus.ReturnsCustomerSignatureMissing;
            result.AddError(
                Name,
                $"{nameof(data.ReturnsSignatures)} must contain a customer signature.",
                logger,
                LogLevel.Error);
        }
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
        Success = 1,
        ProductsDriverSignatureMissing = 2,
        ProductsCustomerSignatureMissing = 3,
        CustomerNameMissing = 4,
        ProductsTotalQuantityInvalid = 5,
        ProductsTotalPriceInvalid = 6,
        ProductsMissing = 7,
        ReturnReasonMissing = 8,
        ReturnsDriverSignatureMissing = 9,
        ReturnsCustomerSignatureMissing = 10
    }
}
