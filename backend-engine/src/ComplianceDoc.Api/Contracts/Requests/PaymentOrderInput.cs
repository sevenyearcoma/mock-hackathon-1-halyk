using System.Text.Json.Serialization;

namespace ComplianceDoc.Api.Contracts.Requests;

public sealed class PaymentOrderInput
{
    [JsonPropertyName("pay_date")]
    public string? Date { get; set; }

    [JsonPropertyName("pay_payer_name")]
    public string? PayerName { get; set; }

    [JsonPropertyName("pay_payer_bin")]
    public string? PayerBin { get; set; }

    [JsonPropertyName("pay_payer_bank")]
    public string? PayerBank { get; set; }

    [JsonPropertyName("pay_payer_iban")]
    public string? PayerIban { get; set; }

    [JsonPropertyName("pay_receiver_name")]
    public string? ReceiverName { get; set; }

    [JsonPropertyName("pay_receiver_bin")]
    public string? ReceiverBin { get; set; }

    [JsonPropertyName("pay_receiver_bank")]
    public string? ReceiverBank { get; set; }

    [JsonPropertyName("pay_receiver_iban")]
    public string? ReceiverIban { get; set; }

    [JsonPropertyName("pay_amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("pay_currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("pay_purpose")]
    public string? Purpose { get; set; }
}
