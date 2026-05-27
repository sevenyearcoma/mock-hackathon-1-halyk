using System.Text.Json.Serialization;

namespace ComplianceDoc.Api.Contracts.Requests;

public sealed class InvoiceInput
{
    [JsonPropertyName("inv_number")]
    public string? Number { get; set; }

    [JsonPropertyName("inv_date")]
    public string? Date { get; set; }

    [JsonPropertyName("inv_due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("inv_currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("inv_subtotal")]
    public decimal? Subtotal { get; set; }

    [JsonPropertyName("inv_vat")]
    public decimal? Vat { get; set; }

    [JsonPropertyName("inv_total")]
    public decimal? Total { get; set; }

    [JsonPropertyName("inv_seller_name")]
    public string? SellerName { get; set; }

    [JsonPropertyName("inv_seller_bin")]
    public string? SellerBin { get; set; }

    [JsonPropertyName("inv_seller_bank")]
    public string? SellerBank { get; set; }

    [JsonPropertyName("inv_seller_iban")]
    public string? SellerIban { get; set; }

    [JsonPropertyName("inv_buyer_name")]
    public string? BuyerName { get; set; }

    [JsonPropertyName("inv_buyer_bin")]
    public string? BuyerBin { get; set; }

    [JsonPropertyName("inv_contract_no")]
    public string? ContractNo { get; set; }

    [JsonPropertyName("inv_contract_date")]
    public string? ContractDate { get; set; }
}
