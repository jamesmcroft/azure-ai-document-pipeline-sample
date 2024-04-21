using AIDocumentPipeline.Shared.Serialization;

namespace AIDocumentPipeline.Invoices;

public class InvoiceData
{
    public string? CustomerName { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? InvoiceDate { get; set; }

    public IEnumerable<InvoiceDataProduct>? Products { get; set; }

    public double? TotalQuantity { get; set; }

    public double? TotalPrice { get; set; }

    public IEnumerable<InvoiceDataSignature>? Signatures { get; set; }

    public static InvoiceData Empty => new()
    {
        CustomerName = string.Empty,
        InvoiceDate = DateTime.MinValue,
        Products =
            new List<InvoiceDataProduct> { new() { Id = string.Empty, UnitPrice = 0.0, Quantity = 0.0, Total = 0.0 } },
        TotalQuantity = 0,
        TotalPrice = 0,
        Signatures = new List<InvoiceDataSignature>
        {
            new()
            {
                Type = "Distributor",
                SignedOn = DateTime.MinValue
            }
        }
    };

    public class InvoiceDataProduct
    {
        public string? Id { get; set; }

        public double? UnitPrice { get; set; }

        public double Quantity { get; set; }

        public double? Total { get; set; }
    }

    public class InvoiceDataSignature
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public string? Type { get; set; }

        [JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime? SignedOn { get; set; }
    }
}
