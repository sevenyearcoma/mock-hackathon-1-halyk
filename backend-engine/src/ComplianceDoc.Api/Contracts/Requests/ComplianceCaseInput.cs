using System.Text.Json.Serialization;

namespace ComplianceDoc.Api.Contracts.Requests;

public sealed class ComplianceCaseInput
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("invoice")]
    public InvoiceInput Invoice { get; set; } = new();

    [JsonPropertyName("payment_order")]
    public PaymentOrderInput PaymentOrder { get; set; } = new();
}
