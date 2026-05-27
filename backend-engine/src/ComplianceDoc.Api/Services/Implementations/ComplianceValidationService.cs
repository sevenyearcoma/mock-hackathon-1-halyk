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

        AddSanctionsListIssues(issues, inv);

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

        // Rule 4 — Bank details mismatch (IBAN and/or bank name)
        var ibanMismatch =
            !string.IsNullOrWhiteSpace(inv.SellerIban) &&
            !string.IsNullOrWhiteSpace(pay.ReceiverIban) &&
            !Same(inv.SellerIban, pay.ReceiverIban);

        var bankNameMismatch =
            !string.IsNullOrWhiteSpace(inv.SellerBank) &&
            !string.IsNullOrWhiteSpace(pay.ReceiverBank) &&
            !Same(inv.SellerBank, pay.ReceiverBank);

        if (ibanMismatch || bankNameMismatch)
        {
            var evidenceParts = new List<string>();
            if (bankNameMismatch)
                evidenceParts.Add($"Банк продавца (счёт): {inv.SellerBank}; Банк получателя (платёж): {pay.ReceiverBank}");
            if (ibanMismatch)
                evidenceParts.Add($"IBAN продавца: {inv.SellerIban}; IBAN получателя: {pay.ReceiverIban}");

            issues.Add(new RiskIssue
            {
                Type = IssueType.BankDetailsMismatch,
                Severity = RiskSeverity.High,
                ScoreImpact = 40,
                Title = "Bank details mismatch",
                Message = "The bank details on the invoice do not match those on the payment order.",
                Evidence = string.Join("; ", evidenceParts),
                RecommendedAction = "Ask the client to confirm the correct bank name and account number for the transfer."
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
                Code = "sanctions_screening",
                Label = "Seller and buyer are not on sanctions list",
                Passed = !Has(IssueType.SanctionsListMatch),
                Details = Has(IssueType.SanctionsListMatch)
                    ? issues.First(x => x.Type == IssueType.SanctionsListMatch).Evidence
                    : null
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

    private static void AddSanctionsListIssues(List<RiskIssue> issues, ExtractedDocumentFields inv)
    {
        var sellerMatch = FindSanctionMatch(SanctionedSellers, inv.SellerName, inv.SellerBin);
        if (sellerMatch is not null)
            issues.Add(CreateSanctionsIssue("Seller", sellerMatch));

        var buyerMatch = FindSanctionMatch(SanctionedBuyers, inv.BuyerName, inv.BuyerBin);
        if (buyerMatch is not null)
            issues.Add(CreateSanctionsIssue("Buyer", buyerMatch));
    }

    private static RiskIssue CreateSanctionsIssue(string role, SanctionEntry entry) => new()
    {
        Type = IssueType.SanctionsListMatch,
        Severity = RiskSeverity.Critical,
        ScoreImpact = 90,
        Title = $"{role} sanctions list match",
        Message = $"{role} was found in the mock sanctions list.",
        Evidence = $"{role}: {entry.Name} / BIN {entry.Bin}; Sanctions ref: {entry.Reference}",
        RecommendedAction = "Stop automatic processing and escalate the case to a compliance officer for sanctions review."
    };

    private static SanctionEntry? FindSanctionMatch(
        IEnumerable<SanctionEntry> sanctions,
        string? counterpartyName,
        string? counterpartyBin)
    {
        return sanctions.FirstOrDefault(entry =>
            (!string.IsNullOrWhiteSpace(counterpartyBin) && Same(entry.Bin, counterpartyBin)) ||
            (!string.IsNullOrWhiteSpace(counterpartyName) && Same(entry.Name, counterpartyName)));
    }

    private static bool Same(string? a, string? b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private sealed record SanctionEntry(string Name, string Bin, string Reference);

    private static readonly SanctionEntry[] SanctionedSellers =
    {
        new("CaspianTech JSC", "200310078901", "MOCK-SELLER-001"),
        new("GoldStep Finance LLP", "231080012345", "MOCK-SELLER-002"),
        new("AlmaTech Solutions LLP", "240540012345", "MOCK-SELLER-003"),
        new("EastTrade Partners LLP", "211120034567", "MOCK-SELLER-004"),
        new("KazEnergy Services LLP", "200560034890", "MOCK-SELLER-005"),
        new("TechHub Almaty LLP", "230660087654", "MOCK-SELLER-006"),
        new("TechnoParc Innovations LLP", "240890056123", "MOCK-SELLER-007"),
        new("BioPharm Central LLP", "221130045678", "MOCK-SELLER-008"),
        new("MedSupply Kazakhstan LLP", "200770078012", "MOCK-SELLER-009"),
        new("PrimeBuild Construction JSC", "191040067890", "MOCK-SELLER-010"),
        new("UrbanDev Holdings LLP", "220950034567", "MOCK-SELLER-011"),
        new("NurLogistics Group LLP", "190830056789", "MOCK-SELLER-012"),
        new("Zhibek Zholy Trading LLP", "230770089012", "MOCK-SELLER-013"),
        new("AltaiFoods JSC", "190270065432", "MOCK-SELLER-014"),
        new("CentralAsia IT Group JSC", "190640056789", "MOCK-SELLER-015"),
        new("Nomad Digital LLP", "241080054321", "MOCK-SELLER-016"),
        new("SteppeInvest LLP", "180920023456", "MOCK-SELLER-017"),
        new("GreenMarket Retail LLP", "220140098765", "MOCK-SELLER-018"),
        new("AstanaFreight Logistics LLP", "210330023456", "MOCK-SELLER-019"),
        new("SilkRoad Imports LLP", "210450076543", "MOCK-SELLER-020")
    };

    private static readonly SanctionEntry[] SanctionedBuyers =
    {
        new("AlmaTech Solutions LLP", "240540012345", "MOCK-BUYER-001"),
        new("EastTrade Partners LLP", "211120034567", "MOCK-BUYER-002"),
        new("GoldStep Finance LLP", "231080012345", "MOCK-BUYER-003"),
        new("Nomad Digital LLP", "241080054321", "MOCK-BUYER-004"),
        new("Zhibek Zholy Trading LLP", "230770089012", "MOCK-BUYER-005"),
        new("CaspianTech JSC", "200310078901", "MOCK-BUYER-006"),
        new("CentralAsia IT Group JSC", "190640056789", "MOCK-BUYER-007"),
        new("GreenMarket Retail LLP", "220140098765", "MOCK-BUYER-008"),
        new("NurLogistics Group LLP", "190830056789", "MOCK-BUYER-009"),
        new("UrbanDev Holdings LLP", "220950034567", "MOCK-BUYER-010"),
        new("AstanaFreight Logistics LLP", "210330023456", "MOCK-BUYER-011"),
        new("SilkRoad Imports LLP", "210450076543", "MOCK-BUYER-012"),
        new("TechnoParc Innovations LLP", "240890056123", "MOCK-BUYER-013"),
        new("PrimeBuild Construction JSC", "191040067890", "MOCK-BUYER-014"),
        new("BioPharm Central LLP", "221130045678", "MOCK-BUYER-015"),
        new("MedSupply Kazakhstan LLP", "200770078012", "MOCK-BUYER-016"),
        new("AltaiFoods JSC", "190270065432", "MOCK-BUYER-017"),
        new("SteppeInvest LLP", "180920023456", "MOCK-BUYER-018"),
        new("TechHub Almaty LLP", "230660087654", "MOCK-BUYER-019"),
        new("KazEnergy Services LLP", "200560034890", "MOCK-BUYER-020")
    };

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
