using AIDocumentPipeline.Shared;
using AIDocumentPipeline.Shared.Observability;
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

        var validationResult = input.Validate();

        var result = new Result();
        result.Merge(validationResult);

        if (!result.IsValid)
        {
            logger.LogError("Invalid input: {ValidationErrors}", result);
            return Task.FromResult(result);
        }

        var data = input.Data!;

        if (string.IsNullOrWhiteSpace(data.CustomerName))
        {
            result.Status |= ResultStatus.CustomerNameMissing;
            result.AddError($"{nameof(data.CustomerName)} is required.");
        }

        if (data.Signatures == null)
        {
            result.Status |= ResultStatus.DistributorSignatureMissing | ResultStatus.CustomerSignatureMissing;
            result.AddError($"{nameof(data.Signatures)} is required.");
        }

        if (data.Signatures != null)
        {
            var distributorSignature = data.Signatures
                .FirstOrDefault(x => x.Type == InvoiceData.InvoiceDataSignatureType.Distributor);
            if (distributorSignature?.SignedOn == null ||
                distributorSignature.SignedOn.Value == DateTime.MinValue)
            {
                result.Status |= ResultStatus.DistributorSignatureMissing;
                result.AddError($"{nameof(data.Signatures)} must contain a distributor signature.");
            }

            var customerSignature = data.Signatures
                .FirstOrDefault(x => x.Type == InvoiceData.InvoiceDataSignatureType.Customer);
            if (customerSignature?.SignedOn == null ||
                customerSignature.SignedOn.Value == DateTime.MinValue)
            {
                result.Status |= ResultStatus.CustomerSignatureMissing;
                result.AddError($"{nameof(data.Signatures)} must contain a customer signature.");
            }
        }

        if (data.Products != null)
        {
            if (!data.Products.Sum(x => x.Quantity).Equals(data.TotalQuantity))
            {
                result.Status |= ResultStatus.ProductsTotalQuantityInvalid;
                result.AddError($"{nameof(data.Products)} quantity total must match {nameof(data.TotalQuantity)}.");
            }

            if (!data.Products.Sum(x => x.Total).Equals(data.TotalPrice))
            {
                result.Status |= ResultStatus.ProductsTotalPriceInvalid;
                result.AddError($"{nameof(data.Products)} price total must match {nameof(data.TotalPrice)}.");
            }
        }

        if (result.IsValid)
        {
            result.Status = ResultStatus.Success;
        }

        return Task.FromResult(result);
    }

    public class Request : BaseWorkflowRequest
    {
        public InvoiceData? Data { get; set; }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (Data == null)
            {
                result.AddError($"{nameof(Data)} is required.");
            }

            return result;
        }
    }

    public class Result : ValidationResult
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
