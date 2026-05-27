using ComplianceDoc.Api.Contracts.Requests;

namespace ComplianceDoc.Api.Controllers;

internal static class DemoData
{
    internal static List<ComplianceCaseInput> GetDemoCases() =>
    [
        // Case 1 — Clean: all fields match, no issues expected (Low risk)
        new ComplianceCaseInput
        {
            Id = 99,
            Invoice = new InvoiceInput
            {
                Number = "INV-2026-0099",
                Date = "26 Mar 2026",
                DueDate = "02 Apr 2026",
                Currency = "EUR",
                Subtotal = 7000,
                Vat = 0,
                Total = 7000,
                SellerName = "Nomad Digital LLP",
                SellerBin = "241080054321",
                SellerBank = "ForteBank JSC",
                SellerIban = "KZ246010000054321098",
                BuyerName = "GoldStep Finance LLP",
                BuyerBin = "231080012345",
                ContractNo = "GF-ND-02/2026",
                ContractDate = "2026-01-13"
            },
            PaymentOrder = new PaymentOrderInput
            {
                Date = "03 Apr 2026",
                PayerName = "GoldStep Finance LLP",
                PayerBin = "231080012345",
                PayerBank = "Halyk Bank Kazakhstan JSC",
                PayerIban = "KZ859650000012345099",
                ReceiverName = "Nomad Digital LLP",
                ReceiverBin = "241080054321",
                ReceiverBank = "ForteBank JSC",
                ReceiverIban = "KZ246010000054321098",
                Amount = 7000,
                Currency = "EUR",
                Purpose = "Payment for accounting services under Contract No. GF-ND-02/2026, Invoice No. INV-2026-0099"
            }
        },

        // Case 2 — Broken: amount mismatch, date issue, vague purpose, missing refs, IBAN mismatch
        new ComplianceCaseInput
        {
            Id = 142,
            Invoice = new InvoiceInput
            {
                Number = "INV-2026-0147",
                Date = "27 May 2026",
                DueDate = "03 Jun 2026",
                Currency = "USD",
                Subtotal = 10500,
                Vat = 0,
                Total = 10500,
                SellerName = "AlmaTech Solutions LLP",
                SellerBin = "240340012345",
                SellerBank = "Kaspi Bank JSC",
                SellerIban = "KZ12345600000099",
                BuyerName = "GreenMarket Retail LLP",
                BuyerBin = "231080099887",
                ContractNo = "GM-AT-05/2026",
                ContractDate = "2026-05-20"
            },
            PaymentOrder = new PaymentOrderInput
            {
                Date = "15 May 2026",           // earlier than invoice date — StrangeDate
                PayerName = "GreenMarket Retail LLP",
                PayerBin = "231080099887",
                PayerBank = "Halyk Bank Kazakhstan JSC",
                PayerIban = "KZ859650000012345099",
                ReceiverName = "AlmaTech Solutions LLP",
                ReceiverBin = "240340012345",
                ReceiverBank = "Forte Bank JSC",
                ReceiverIban = "KZ99998800000011",  // differs from seller IBAN — BankDetailsMismatch
                Amount = 10000,                    // differs from invoice total 10500 — AmountMismatch
                Currency = "USD",
                Purpose = "Payment"               // vague, no contract/invoice ref
            }
        }
    ];
}
