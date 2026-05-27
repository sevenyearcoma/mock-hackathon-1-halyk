using System.Globalization;
using ComplianceDoc.Api.Contracts.Requests;
using ComplianceDoc.Api.Domain.Enums;
using ComplianceDoc.Api.Domain.Models;
using ComplianceDoc.Api.Services.Interfaces;

namespace ComplianceDoc.Api.Services.Implementations;

public sealed class ComplianceValidationService : IComplianceValidationService
{
    private readonly IRiskScoringService _scoring;
    private readonly IClientMessageService _messaging;

    public ComplianceValidationService(IRiskScoringService scoring, IClientMessageService messaging)
    {
        _scoring = scoring;
        _messaging = messaging;
    }

    public ComplianceCheck CreateCheck(ComplianceCaseInput input)
    {
        var invoice = MapInvoice(input.Invoice);
        var payment = MapPaymentOrder(input.PaymentOrder);
        var issues = RunRules(invoice, payment);
        var riskSummary = _scoring.Calculate(issues);
        var checklist = BuildChecklist(invoice, payment, issues);

        return new ComplianceCheck
        {
            SourceId = input.Id,
            CaseNumber = $"CASE-{input.Id}",
            ClientName = invoice.BuyerName ?? payment.PayerName ?? "Unknown",
            Invoice = invoice,
            PaymentOrder = payment,
            Checklist = checklist,
            RiskSummary = riskSummary,
            Explanation = _messaging.GenerateExplanation(riskSummary),
            DraftClientMessage = _messaging.GenerateClientMessage(riskSummary)
        };
    }

    // ── Mapping ──────────────────────────────────────────────────────────

    private static ExtractedDocumentFields MapInvoice(InvoiceInput inv) => new()
    {
        DocumentType = DocumentType.Invoice,
        DocumentNumber = inv.Number,
        DocumentDate = ParseDate(inv.Date),
        DueDate = ParseDate(inv.DueDate),
        ContractNumber = inv.ContractNo,
        ContractDate = ParseDate(inv.ContractDate),
        SellerName = inv.SellerName,
        SellerBin = inv.SellerBin,
        SellerBank = inv.SellerBank,
        SellerIban = inv.SellerIban,
        BuyerName = inv.BuyerName,
        BuyerBin = inv.BuyerBin,
        Subtotal = inv.Subtotal,
        Vat = inv.Vat,
        Amount = inv.Total,
        Currency = inv.Currency?.ToUpperInvariant(),
        InvoiceNumber = inv.Number
    };

    private static ExtractedDocumentFields MapPaymentOrder(PaymentOrderInput pay) => new()
    {
        DocumentType = DocumentType.PaymentOrder,
        DocumentDate = ParseDate(pay.Date),
        PayerName = pay.PayerName,
        PayerBin = pay.PayerBin,
        PayerBank = pay.PayerBank,
        PayerIban = pay.PayerIban,
        ReceiverName = pay.ReceiverName,
        ReceiverBin = pay.ReceiverBin,
        ReceiverBank = pay.ReceiverBank,
        ReceiverIban = pay.ReceiverIban,
        Amount = pay.Amount,
        Currency = pay.Currency?.ToUpperInvariant(),
        PaymentPurpose = pay.Purpose
    };

    // ── Rules ─────────────────────────────────────────────────────────────

    private static List<RiskIssue> RunRules(ExtractedDocumentFields inv, ExtractedDocumentFields pay)
    {
        var issues = new List<RiskIssue>();

        // Rule 1 — Amount mismatch
        if (inv.Amount.HasValue && pay.Amount.HasValue && inv.Amount != pay.Amount)
        {
            issues.Add(new RiskIssue
            {
                Type = IssueType.AmountMismatch,
                Severity = RiskSeverity.High,
                ScoreImpact = 35,
                Title = "Amount mismatch",
                Message = "The invoice total does not match the payment order amount.",
                Evidence = $"Invoice total: {inv.Amount} {inv.Currency}; Payment amount: {pay.Amount} {pay.Currency}",
                RecommendedAction = "Request a corrected invoice or additional agreement from the client."
            });
        }

        // Rule 2 — Currency mismatch
        if (!Same(inv.Currency, pay.Currency))
        {
            issues.Add(new RiskIssue
            {
                Type = IssueType.CurrencyMismatch,
                Severity = RiskSeverity.High,
                ScoreImpact = 35,
                Title = "Currency mismatch",
                Message = "The currency differs between the invoice and the payment order.",
                Evidence = $"Invoice currency: {inv.Currency ?? "—"}; Payment currency: {pay.Currency ?? "—"}",
                RecommendedAction = "Clarify the correct payment currency with the client."
            });
        }

        // Rule 3 — Counterparty mismatch (seller ↔ receiver, buyer ↔ payer)
        var sellerReceiverMismatch =
            !Same(inv.SellerName, pay.ReceiverName) &&
            !Same(inv.SellerBin, pay.ReceiverBin);

        var buyerPayerMismatch =
            !Same(inv.BuyerName, pay.PayerName) &&
            !Same(inv.BuyerBin, pay.PayerBin);

        if (sellerReceiverMismatch || buyerPayerMismatch)
        {
            var evidence = new List<string>();
            if (sellerReceiverMismatch)
                evidence.Add($"Seller: {inv.SellerName} / Receiver: {pay.ReceiverName}");
            if (buyerPayerMismatch)
                evidence.Add($"Buyer: {inv.BuyerName} / Payer: {pay.PayerName}");

            issues.Add(new RiskIssue
            {
                Type = IssueType.CounterpartyMismatch,
                Severity = RiskSeverity.High,
                ScoreImpact = 40,
                Title = "Counterparty mismatch",
                Message = "The parties in the invoice and the payment order do not match.",
                Evidence = string.Join("; ", evidence),
                RecommendedAction = "Verify the correct counterparty names and BINs with the client."
            });
        }

        // Rule 4 — Bank details mismatch (seller IBAN ↔ receiver IBAN)
        if (!string.IsNullOrWhiteSpace(inv.SellerIban) &&
            !string.IsNullOrWhiteSpace(pay.ReceiverIban) &&
            !Same(inv.SellerIban, pay.ReceiverIban))
        {
            issues.Add(new RiskIssue
            {
                Type = IssueType.BankDetailsMismatch,
                Severity = RiskSeverity.High,
                ScoreImpact = 40,
                Title = "Bank details mismatch",
                Message = "The seller's IBAN on the invoice does not match the receiver's IBAN on the payment order.",
                Evidence = $"Invoice seller IBAN: {inv.SellerIban}; Payment receiver IBAN: {pay.ReceiverIban}",
                RecommendedAction = "Ask the client to confirm the correct bank account for the transfer."
            });
        }

        // Rule 5 — Vague payment purpose
        var vague = new[] { "payment", "services", "consulting", "invoice", "other services" };
        if (string.IsNullOrWhiteSpace(pay.PaymentPurpose) ||
            vague.Contains(pay.PaymentPurpose.Trim().ToLowerInvariant()))
        {
            issues.Add(new RiskIssue
            {
                Type = IssueType.VaguePaymentPurpose,
                Severity = RiskSeverity.Medium,
                ScoreImpact = 20,
                Title = "Vague payment purpose",
                Message = "The payment purpose is too generic and does not explain the business reason for the transfer.",
                Evidence = $"Payment purpose: {pay.PaymentPurpose ?? "missing"}",
                RecommendedAction = "Ask the client to include contract number, invoice number and service description in the payment purpose."
            });
        }

        // Rule 6 — Strange date (payment before invoice)
        if (pay.DocumentDate.HasValue && inv.DocumentDate.HasValue &&
            pay.DocumentDate.Value < inv.DocumentDate.Value)
        {
            issues.Add(new RiskIssue
            {
                Type = IssueType.StrangeDate,
                Severity = RiskSeverity.Medium,
                ScoreImpact = 15,
                Title = "Payment order date is earlier than invoice date",
                Message = "The payment order was created before the invoice date.",
                Evidence = $"Payment order date: {pay.DocumentDate}; Invoice date: {inv.DocumentDate}",
                RecommendedAction = "Ask the client to clarify the document chronology or provide updated documents."
            });
        }

        // Rule 7 — Missing contract reference in payment purpose
        var contractRef = inv.ContractNumber;
        if (!string.IsNullOrWhiteSpace(contractRef) &&
            !string.IsNullOrWhiteSpace(pay.PaymentPurpose) &&
            !pay.PaymentPurpose.Contains(contractRef, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new RiskIssue
            {
                Type = IssueType.MissingContractReference,
                Severity = RiskSeverity.Medium,
                ScoreImpact = 15,
                Title = "Missing contract reference",
                Message = "The payment purpose does not mention the contract number.",
                Evidence = $"Contract number: {contractRef}; Payment purpose: {pay.PaymentPurpose}",
                RecommendedAction = "Ask the client to add the contract number to the payment purpose."
            });
        }
        else if (string.IsNullOrWhiteSpace(contractRef))
        {
            issues.Add(new RiskIssue
            {
                Type = IssueType.MissingContractReference,
                Severity = RiskSeverity.Medium,
                ScoreImpact = 15,
                Title = "Missing contract reference",
                Message = "The payment order does not mention a contract number.",
                Evidence = "No contract number found in invoice or payment order.",
                RecommendedAction = "Ask the client to provide the contract number."
            });
        }

        // Rule 8 — Missing invoice reference in payment purpose
        var invoiceRef = inv.InvoiceNumber;
        if (!string.IsNullOrWhiteSpace(invoiceRef) &&
            !string.IsNullOrWhiteSpace(pay.PaymentPurpose) &&
            !pay.PaymentPurpose.Contains(invoiceRef, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new RiskIssue
            {
                Type = IssueType.MissingInvoiceReference,
                Severity = RiskSeverity.Low,
                ScoreImpact = 10,
                Title = "Missing invoice reference",
                Message = "The payment purpose does not mention the invoice number.",
                Evidence = $"Invoice number: {invoiceRef}; Payment purpose: {pay.PaymentPurpose}",
                RecommendedAction = "Ask the client to add the invoice number to the payment purpose."
            });
        }

        return issues;
    }

    // ── Checklist ─────────────────────────────────────────────────────────

    private static List<ChecklistItem> BuildChecklist(
        ExtractedDocumentFields inv,
        ExtractedDocumentFields pay,
        List<RiskIssue> issues)
    {
        bool Has(IssueType t) => issues.Any(x => x.Type == t);

        return new List<ChecklistItem>
        {
            new() { Code = "invoice_present",            Label = "Invoice present",                      Passed = true },
            new() { Code = "payment_order_present",      Label = "Payment order present",                Passed = true },
            new() { Code = "amount_present",             Label = "Amount present on both documents",     Passed = inv.Amount.HasValue && pay.Amount.HasValue },
            new() { Code = "currency_present",           Label = "Currency present on both documents",   Passed = !string.IsNullOrWhiteSpace(inv.Currency) && !string.IsNullOrWhiteSpace(pay.Currency) },
            new()
            {
                Code = "amount_match",
                Label = "Amounts match",
                Passed = !Has(IssueType.AmountMismatch),
                Details = Has(IssueType.AmountMismatch) ? $"Invoice: {inv.Amount} {inv.Currency}; Payment: {pay.Amount} {pay.Currency}" : null
            },
            new()
            {
                Code = "currency_match",
                Label = "Currencies match",
                Passed = !Has(IssueType.CurrencyMismatch),
                Details = Has(IssueType.CurrencyMismatch) ? $"Invoice: {inv.Currency}; Payment: {pay.Currency}" : null
            },
            new()
            {
                Code = "counterparty_match",
                Label = "Counterparties match",
                Passed = !Has(IssueType.CounterpartyMismatch),
                Details = Has(IssueType.CounterpartyMismatch) ? "Seller/buyer names or BINs do not match payer/receiver." : null
            },
            new()
            {
                Code = "bank_details_match",
                Label = "Bank details match",
                Passed = !Has(IssueType.BankDetailsMismatch),
                Details = Has(IssueType.BankDetailsMismatch) ? "Seller IBAN differs from receiver IBAN." : null
            },
            new()
            {
                Code = "payment_purpose_clear",
                Label = "Payment purpose is specific",
                Passed = !Has(IssueType.VaguePaymentPurpose),
                Details = Has(IssueType.VaguePaymentPurpose) ? $"Purpose: {pay.PaymentPurpose ?? "missing"}" : null
            },
            new()
            {
                Code = "payment_date_valid",
                Label = "Payment date is not earlier than invoice date",
                Passed = !Has(IssueType.StrangeDate),
                Details = Has(IssueType.StrangeDate) ? $"Payment: {pay.DocumentDate}; Invoice: {inv.DocumentDate}" : null
            },
            new()
            {
                Code = "contract_reference_present",
                Label = "Contract number referenced in payment purpose",
                Passed = !Has(IssueType.MissingContractReference),
                Details = Has(IssueType.MissingContractReference) ? $"Contract: {inv.ContractNumber ?? "—"}" : null
            },
            new()
            {
                Code = "invoice_reference_present",
                Label = "Invoice number referenced in payment purpose",
                Passed = !Has(IssueType.MissingInvoiceReference),
                Details = Has(IssueType.MissingInvoiceReference) ? $"Invoice: {inv.InvoiceNumber ?? "—"}" : null
            }
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static bool Same(string? a, string? b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static readonly string[] DateFormats =
    {
        "dd MMM yyyy", "d MMM yyyy", "yyyy-MM-dd", "dd.MM.yyyy", "d.MM.yyyy"
    };

    internal static DateOnly? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        foreach (var fmt in DateFormats)
        {
            if (DateOnly.TryParseExact(s, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
        }
        return null;
    }
}
