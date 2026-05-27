# CLAUDE.md — Backend Guide for ComplianceDoc Copilot (.NET)

## Project Context

We are building a hackathon MVP for **ComplianceDoc Copilot** — an AI-assisted backend for currency control and compliance document checks.

The product helps a bank employee review documents for a foreign currency payment:

- Contract
- Invoice
- Payment order

The backend must extract key fields, compare them across documents, detect inconsistencies, calculate a simple risk score, explain the issues, and generate a draft message to the client.

Important: the system is a **copilot**, not an autonomous decision-maker. It must never approve, reject, block, or make a final legal/compliance decision by itself. Final responsibility always stays with a human bank employee.

---

## MVP Goal

Build a .NET backend that supports this demo flow:

```text
Document input
    ↓
Field extraction
    ↓
Structured JSON
    ↓
Cross-document comparison
    ↓
Validation checklist
    ↓
Risk scoring
    ↓
Plain-language explanation
    ↓
Draft client message
    ↓
Human review
```

The MVP should work reliably on a prepared demo case with intentional issues:

1. Invoice amount differs from contract/payment order.
2. Payment order date is earlier than invoice date.
3. Payment purpose is too vague.
4. Service description differs between contract and invoice.
5. Payment order does not mention contract/invoice number.

---

## Recommended Stack

Use:

```text
.NET 8
ASP.NET Core Web API
C#
Minimal APIs or Controllers
System.Text.Json
Swagger / OpenAPI
In-memory storage for hackathon MVP
```

For hackathon speed, prefer:

```text
ASP.NET Core Web API + hardcoded demo parser + rule engine
```

Do not overbuild.

---

## Core Backend Responsibilities

The backend should expose APIs for:

1. Creating a compliance check from raw document texts.
2. Returning extracted fields.
3. Returning comparison results.
4. Returning checklist results.
5. Returning risk score and detected issues.
6. Returning generated draft message to client.
7. Marking a case as reviewed by a human.

---

## High-Level Architecture

```text
Controllers / Minimal API Endpoints
        ↓
Application Services
        ↓
Document Extraction Service
        ↓
Normalization Service
        ↓
Validation Rule Engine
        ↓
Risk Scoring Service
        ↓
Explanation / Draft Message Service
        ↓
Response DTO
```

Recommended folder structure:

```text
src/
  ComplianceDoc.Api/
    Controllers/
      ComplianceChecksController.cs

    Contracts/
      Requests/
        CreateComplianceCheckRequest.cs
        ReviewComplianceCheckRequest.cs

      Responses/
        ComplianceCheckResponse.cs
        ExtractedFieldsResponse.cs
        RiskIssueResponse.cs
        ChecklistItemResponse.cs

    Domain/
      Enums/
        DocumentType.cs
        RiskLevel.cs
        RiskSeverity.cs
        IssueType.cs

      Models/
        ComplianceCheck.cs
        DocumentInput.cs
        ExtractedDocumentFields.cs
        PartyInfo.cs
        RiskIssue.cs
        ChecklistItem.cs
        RiskSummary.cs

    Services/
      Interfaces/
        IDocumentExtractionService.cs
        IComplianceValidationService.cs
        IRiskScoringService.cs
        IClientMessageService.cs
        IComplianceCheckStore.cs

      Implementations/
        RegexDocumentExtractionService.cs
        ComplianceValidationService.cs
        RiskScoringService.cs
        ClientMessageService.cs
        InMemoryComplianceCheckStore.cs

    Program.cs
```

---

## Domain Enums

```csharp
public enum DocumentType
{
    Contract,
    Invoice,
    PaymentOrder
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum RiskSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum IssueType
{
    AmountMismatch,
    CurrencyMismatch,
    CounterpartyMismatch,
    BankDetailsMismatch,
    VaguePaymentPurpose,
    StrangeDate,
    MissingRequiredField,
    MissingContractReference,
    MissingInvoiceReference,
    ServiceDescriptionMismatch
}
```

---

## Request DTOs

```csharp
public sealed class CreateComplianceCheckRequest
{
    public string ClientName { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;

    public string ContractText { get; set; } = string.Empty;
    public string InvoiceText { get; set; } = string.Empty;
    public string PaymentOrderText { get; set; } = string.Empty;
}

public sealed class ReviewComplianceCheckRequest
{
    public bool ApprovedByHuman { get; set; }
    public string? HumanComment { get; set; }
    public string? EditedClientMessage { get; set; }
}
```

---

## Core Models

```csharp
public sealed class ExtractedDocumentFields
{
    public DocumentType DocumentType { get; set; }

    public string? DocumentNumber { get; set; }
    public DateOnly? DocumentDate { get; set; }

    public string? ContractNumber { get; set; }
    public string? InvoiceNumber { get; set; }

    public string? SellerName { get; set; }
    public string? BuyerName { get; set; }

    public string? PayerName { get; set; }
    public string? ReceiverName { get; set; }

    public decimal? Amount { get; set; }
    public string? Currency { get; set; }

    public string? ServiceDescription { get; set; }
    public string? PaymentPurpose { get; set; }

    public string? Iban { get; set; }
    public string? BankName { get; set; }
}

public sealed class RiskIssue
{
    public IssueType Type { get; set; }
    public RiskSeverity Severity { get; set; }
    public int ScoreImpact { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

public sealed class ChecklistItem
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Details { get; set; }
}

public sealed class RiskSummary
{
    public int Score { get; set; }
    public RiskLevel Level { get; set; }
    public List<RiskIssue> Issues { get; set; } = new();
}

public sealed class ComplianceCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CaseNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ExtractedDocumentFields Contract { get; set; } = new();
    public ExtractedDocumentFields Invoice { get; set; } = new();
    public ExtractedDocumentFields PaymentOrder { get; set; } = new();

    public List<ChecklistItem> Checklist { get; set; } = new();
    public RiskSummary RiskSummary { get; set; } = new();

    public string Explanation { get; set; } = string.Empty;
    public string DraftClientMessage { get; set; } = string.Empty;

    public bool ReviewedByHuman { get; set; }
    public string? HumanComment { get; set; }
    public string? FinalClientMessage { get; set; }
}
```

---

## API Endpoints

### Create compliance check

```http
POST /api/compliance-checks
```

Request:

```json
{
  "clientName": "GreenMarket Retail LLP",
  "caseNumber": "VC-2026-0142",
  "contractText": "...",
  "invoiceText": "...",
  "paymentOrderText": "..."
}
```

### Get full compliance check

```http
GET /api/compliance-checks/{id}
```

Should return:

- extracted fields;
- checklist;
- issues;
- risk summary;
- explanation;
- draft client message;
- human review status.

### Review compliance check

```http
POST /api/compliance-checks/{id}/review
```

Request:

```json
{
  "approvedByHuman": true,
  "humanComment": "Checked by compliance specialist.",
  "editedClientMessage": "Final edited message..."
}
```

This endpoint only records human review. It must not imply automatic legal approval.

### Demo endpoint

```http
POST /api/compliance-checks/demo
```

Creates a check from hardcoded demo documents. This endpoint is important for stable hackathon demos.

---

## Controller Example

```csharp
[ApiController]
[Route("api/compliance-checks")]
public sealed class ComplianceChecksController : ControllerBase
{
    private readonly IComplianceValidationService _validationService;
    private readonly IComplianceCheckStore _store;

    public ComplianceChecksController(
        IComplianceValidationService validationService,
        IComplianceCheckStore store)
    {
        _validationService = validationService;
        _store = store;
    }

    [HttpPost]
    public async Task<ActionResult<ComplianceCheck>> Create(
        [FromBody] CreateComplianceCheckRequest request,
        CancellationToken cancellationToken)
    {
        var check = await _validationService.CreateCheckAsync(request, cancellationToken);
        await _store.SaveAsync(check, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = check.Id }, check);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ComplianceCheck>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var check = await _store.GetByIdAsync(id, cancellationToken);

        if (check is null)
        {
            return NotFound();
        }

        return Ok(check);
    }

    [HttpPost("{id:guid}/review")]
    public async Task<ActionResult<ComplianceCheck>> Review(
        Guid id,
        [FromBody] ReviewComplianceCheckRequest request,
        CancellationToken cancellationToken)
    {
        var check = await _store.GetByIdAsync(id, cancellationToken);

        if (check is null)
        {
            return NotFound();
        }

        check.ReviewedByHuman = request.ApprovedByHuman;
        check.HumanComment = request.HumanComment;
        check.FinalClientMessage = request.EditedClientMessage ?? check.DraftClientMessage;

        await _store.SaveAsync(check, cancellationToken);

        return Ok(check);
    }
}
```

---

## Document Extraction Service

For the MVP, use a simple regex/demo parser. Do not build full OCR unless the team has extra time.

```csharp
public interface IDocumentExtractionService
{
    ExtractedDocumentFields Extract(DocumentType documentType, string text);
}
```

Example implementation:

```csharp
public sealed class RegexDocumentExtractionService : IDocumentExtractionService
{
    public ExtractedDocumentFields Extract(DocumentType documentType, string text)
    {
        return documentType switch
        {
            DocumentType.Contract => ExtractContract(text),
            DocumentType.Invoice => ExtractInvoice(text),
            DocumentType.PaymentOrder => ExtractPaymentOrder(text),
            _ => throw new ArgumentOutOfRangeException(nameof(documentType))
        };
    }

    private static ExtractedDocumentFields ExtractContract(string text)
    {
        return new ExtractedDocumentFields
        {
            DocumentType = DocumentType.Contract,
            ContractNumber = ExtractAfter(text, "Contract №"),
            DocumentDate = ExtractDate(text),
            SellerName = ExtractAfter(text, "Supplier:"),
            BuyerName = ExtractAfter(text, "Buyer:"),
            Amount = ExtractAmount(text),
            Currency = ExtractCurrency(text),
            ServiceDescription = ExtractAfter(text, "Service:")
        };
    }

    private static ExtractedDocumentFields ExtractInvoice(string text)
    {
        return new ExtractedDocumentFields
        {
            DocumentType = DocumentType.Invoice,
            InvoiceNumber = ExtractAfter(text, "Invoice №"),
            DocumentDate = ExtractDate(text),
            SellerName = ExtractAfter(text, "Seller:"),
            BuyerName = ExtractAfter(text, "Buyer:"),
            Amount = ExtractAmount(text),
            Currency = ExtractCurrency(text),
            ServiceDescription = ExtractAfter(text, "Description:")
        };
    }

    private static ExtractedDocumentFields ExtractPaymentOrder(string text)
    {
        return new ExtractedDocumentFields
        {
            DocumentType = DocumentType.PaymentOrder,
            DocumentDate = ExtractDate(text),
            PayerName = ExtractAfter(text, "Payer:"),
            ReceiverName = ExtractAfter(text, "Receiver:"),
            Amount = ExtractAmount(text),
            Currency = ExtractCurrency(text),
            PaymentPurpose = ExtractAfter(text, "Payment purpose:"),
            ContractNumber = ExtractContractReference(text),
            InvoiceNumber = ExtractInvoiceReference(text)
        };
    }

    private static string? ExtractAfter(string text, string marker)
    {
        var line = text
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => x.StartsWith(marker, StringComparison.OrdinalIgnoreCase));

        return line is null
            ? null
            : line.Replace(marker, "", StringComparison.OrdinalIgnoreCase).Trim();
    }

    private static decimal? ExtractAmount(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            text,
            @"(?i)(amount|total contract amount|total amount):?\s*([\d,\.]+)");

        if (!match.Success)
        {
            return null;
        }

        var raw = match.Groups[2].Value.Replace(",", "");
        return decimal.TryParse(raw, out var value) ? value : null;
    }

    private static string? ExtractCurrency(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\b(USD|EUR|KZT|RUB|CNY)\b");
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static DateOnly? ExtractDate(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\b(\d{2})\.(\d{2})\.(\d{4})\b");

        if (!match.Success)
        {
            return null;
        }

        var day = int.Parse(match.Groups[1].Value);
        var month = int.Parse(match.Groups[2].Value);
        var year = int.Parse(match.Groups[3].Value);

        return new DateOnly(year, month, day);
    }

    private static string? ExtractContractReference(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"GM-AT-\d{2}/\d{4}");
        return match.Success ? match.Value : null;
    }

    private static string? ExtractInvoiceReference(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"INV-\d{4}-\d{4}");
        return match.Success ? match.Value : null;
    }
}
```

---

## Validation Rules

Implement these in `ComplianceValidationService`.

### Rule 1 — Amount mismatch

```csharp
if (contract.Amount != invoice.Amount || invoice.Amount != payment.Amount)
{
    issues.Add(new RiskIssue
    {
        Type = IssueType.AmountMismatch,
        Severity = RiskSeverity.High,
        ScoreImpact = 35,
        Title = "Amount mismatch",
        Message = "The amount differs between contract, invoice and payment order.",
        Evidence = $"Contract: {contract.Amount} {contract.Currency}; Invoice: {invoice.Amount} {invoice.Currency}; Payment: {payment.Amount} {payment.Currency}",
        RecommendedAction = "Request corrected invoice or additional agreement from the client."
    });
}
```

### Rule 2 — Currency mismatch

```csharp
if (!Same(contract.Currency, invoice.Currency) || !Same(invoice.Currency, payment.Currency))
{
    issues.Add(new RiskIssue
    {
        Type = IssueType.CurrencyMismatch,
        Severity = RiskSeverity.High,
        ScoreImpact = 35,
        Title = "Currency mismatch",
        Message = "Currency is not consistent across documents.",
        Evidence = $"Contract: {contract.Currency}; Invoice: {invoice.Currency}; Payment: {payment.Currency}",
        RecommendedAction = "Clarify the correct payment currency."
    });
}
```

### Rule 3 — Strange date

```csharp
if (payment.DocumentDate.HasValue && invoice.DocumentDate.HasValue &&
    payment.DocumentDate.Value < invoice.DocumentDate.Value)
{
    issues.Add(new RiskIssue
    {
        Type = IssueType.StrangeDate,
        Severity = RiskSeverity.Medium,
        ScoreImpact = 15,
        Title = "Payment order date is earlier than invoice date",
        Message = "The payment order was created before the invoice date.",
        Evidence = $"Payment order date: {payment.DocumentDate}; Invoice date: {invoice.DocumentDate}",
        RecommendedAction = "Ask the client to clarify the document chronology or provide updated documents."
    });
}
```

### Rule 4 — Vague payment purpose

```csharp
var vaguePurposes = new[] { "payment", "services", "consulting", "invoice", "other services" };

if (string.IsNullOrWhiteSpace(payment.PaymentPurpose) ||
    vaguePurposes.Contains(payment.PaymentPurpose.Trim().ToLowerInvariant()))
{
    issues.Add(new RiskIssue
    {
        Type = IssueType.VaguePaymentPurpose,
        Severity = RiskSeverity.Medium,
        ScoreImpact = 20,
        Title = "Vague payment purpose",
        Message = "The payment purpose is too generic and does not explain the business reason for the transfer.",
        Evidence = $"Payment purpose: {payment.PaymentPurpose ?? "missing"}",
        RecommendedAction = "Ask the client to include contract number, invoice number and service description in the payment purpose."
    });
}
```

### Rule 5 — Missing contract reference in payment order

```csharp
if (string.IsNullOrWhiteSpace(payment.ContractNumber))
{
    issues.Add(new RiskIssue
    {
        Type = IssueType.MissingContractReference,
        Severity = RiskSeverity.Medium,
        ScoreImpact = 15,
        Title = "Missing contract reference",
        Message = "The payment order does not mention the contract number.",
        Evidence = "No contract number found in payment purpose or payment order text.",
        RecommendedAction = "Ask the client to add the contract number to the payment purpose."
    });
}
```

### Rule 6 — Missing invoice reference in payment order

```csharp
if (string.IsNullOrWhiteSpace(payment.InvoiceNumber))
{
    issues.Add(new RiskIssue
    {
        Type = IssueType.MissingInvoiceReference,
        Severity = RiskSeverity.Low,
        ScoreImpact = 10,
        Title = "Missing invoice reference",
        Message = "The payment order does not mention the invoice number.",
        Evidence = "No invoice number found in payment purpose or payment order text.",
        RecommendedAction = "Ask the client to add the invoice number to the payment purpose."
    });
}
```

### Rule 7 — Service description mismatch

```csharp
if (!string.IsNullOrWhiteSpace(contract.ServiceDescription) &&
    !string.IsNullOrWhiteSpace(invoice.ServiceDescription) &&
    !IsSimilarService(contract.ServiceDescription, invoice.ServiceDescription))
{
    issues.Add(new RiskIssue
    {
        Type = IssueType.ServiceDescriptionMismatch,
        Severity = RiskSeverity.Medium,
        ScoreImpact = 15,
        Title = "Service description mismatch",
        Message = "The service description differs between contract and invoice.",
        Evidence = $"Contract: {contract.ServiceDescription}; Invoice: {invoice.ServiceDescription}",
        RecommendedAction = "Ask the client to clarify whether the invoice relates to the same contract service."
    });
}
```

Helper:

```csharp
private static bool Same(string? a, string? b)
{
    return string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}

private static bool IsSimilarService(string a, string b)
{
    var left = a.ToLowerInvariant();
    var right = b.ToLowerInvariant();

    if (left == right)
    {
        return true;
    }

    var sharedKeywords = new[] { "software", "development", "crm", "module", "it" };
    return sharedKeywords.Any(keyword => left.Contains(keyword) && right.Contains(keyword));
}
```

---

## Checklist

Generate checklist items from extracted fields and issues.

```text
contract_present
invoice_present
payment_order_present
amount_present
currency_present
amount_match
currency_match
payment_purpose_clear
payment_date_valid
contract_reference_present
invoice_reference_present
service_description_match
```

Example:

```csharp
new ChecklistItem
{
    Code = "amount_match",
    Label = "Amounts match across documents",
    Passed = !HasIssue(IssueType.AmountMismatch),
    Details = HasIssue(IssueType.AmountMismatch)
        ? "Contract/payment order amount differs from invoice amount."
        : null
}
```

---

## Risk Scoring

Use an explainable additive score.

```text
Amount mismatch: +35
Currency mismatch: +35
Counterparty mismatch: +40
Bank details mismatch: +40
Vague payment purpose: +20
Strange date: +15
Missing contract reference: +15
Missing invoice reference: +10
Service description mismatch: +15
Missing required field: +20
```

Risk level:

```text
0–20: Low
21–50: Medium
51–80: High
81+: Critical
```

Implementation:

```csharp
public sealed class RiskScoringService : IRiskScoringService
{
    public RiskSummary Calculate(List<RiskIssue> issues)
    {
        var score = issues.Sum(x => x.ScoreImpact);
        var cappedScore = Math.Min(score, 100);

        return new RiskSummary
        {
            Score = cappedScore,
            Level = ToRiskLevel(cappedScore),
            Issues = issues
        };
    }

    private static RiskLevel ToRiskLevel(int score)
    {
        return score switch
        {
            <= 20 => RiskLevel.Low,
            <= 50 => RiskLevel.Medium,
            <= 80 => RiskLevel.High,
            _ => RiskLevel.Critical
        };
    }
}
```

---

## Explanation Generation

For hackathon MVP, deterministic text is better than unstable LLM output.

```csharp
public string GenerateExplanation(RiskSummary riskSummary)
{
    if (riskSummary.Issues.Count == 0)
    {
        return "No major inconsistencies were detected. The documents can proceed to standard human review.";
    }

    var lines = new List<string>
    {
        $"Overall risk level: {riskSummary.Level}.",
        $"Risk score: {riskSummary.Score}/100.",
        "",
        "Detected issues:"
    };

    foreach (var issue in riskSummary.Issues)
    {
        lines.Add($"- {issue.Title}: {issue.Message} Recommended action: {issue.RecommendedAction}");
    }

    lines.Add("");
    lines.Add("This is not a final compliance decision. A bank employee must review the result before taking action.");

    return string.Join(Environment.NewLine, lines);
}
```

---

## Draft Client Message

Generate a clear message based on detected issues.

```csharp
public string GenerateClientMessage(ComplianceCheck check)
{
    var issues = check.RiskSummary.Issues;

    if (issues.Count == 0)
    {
        return """
        Hello!

        We have completed the preliminary document review and did not identify major inconsistencies.
        The documents will proceed to standard compliance review.

        Thank you.
        """;
    }

    var builder = new StringBuilder();

    builder.AppendLine("Hello!");
    builder.AppendLine();
    builder.AppendLine("During the preliminary review of the documents for the foreign currency payment, we identified the following points that require clarification:");
    builder.AppendLine();

    for (var i = 0; i < issues.Count; i++)
    {
        builder.AppendLine($"{i + 1}. {issues[i].Message}");
        builder.AppendLine($"   Required action: {issues[i].RecommendedAction}");
    }

    builder.AppendLine();
    builder.AppendLine("Please provide corrected documents or additional clarification so that the review can continue.");
    builder.AppendLine();
    builder.AppendLine("Thank you!");

    return builder.ToString();
}
```

---

## Demo Documents

Use this demo case for `/api/compliance-checks/demo`.

### Contract

```text
Contract № GM-AT-05/2026
Date: 20.05.2026

Supplier: AlmaTech Solutions LLP
Buyer: GreenMarket Retail LLP

Service: Development of internal CRM module.
Total contract amount: 10,000 USD.
Payment is made after invoice issuance.
```

### Invoice

```text
Invoice № INV-2026-0147
Date: 27.05.2026

Seller: AlmaTech Solutions LLP
Buyer: GreenMarket Retail LLP

Description: IT consulting services.
Total amount: 10,500 USD.
Payment due date: 03.06.2026.
```

### Payment Order

```text
Payment order
Date: 15.05.2026

Payer: GreenMarket Retail LLP
Receiver: AlmaTech Solutions LLP
Amount: 10,000 USD
Payment purpose: Payment
```

Expected detected issues:

```text
Amount mismatch
Vague payment purpose
Payment order date earlier than invoice date
Missing contract reference in payment order
Missing invoice reference in payment order
Service description mismatch
```

Expected risk:

```text
Risk score: around 80–100
Risk level: High or Critical
```

---

## Program.cs Registration

Example:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IComplianceCheckStore, InMemoryComplianceCheckStore>();
builder.Services.AddScoped<IDocumentExtractionService, RegexDocumentExtractionService>();
builder.Services.AddScoped<IComplianceValidationService, ComplianceValidationService>();
builder.Services.AddScoped<IRiskScoringService, RiskScoringService>();
builder.Services.AddScoped<IClientMessageService, ClientMessageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
```

---

## Success Criteria

Backend is successful if:

1. `POST /api/compliance-checks/demo` creates a check.
2. Response includes extracted contract, invoice and payment order fields.
3. Response includes checklist items.
4. Response includes detected issues.
5. Response includes risk score and risk level.
6. Response includes explanation.
7. Response includes draft client message.
8. `POST /api/compliance-checks/{id}/review` records human review.
9. Swagger works.
10. Demo does not crash.

---

## Important Product Rules

### Do

- Keep rules explainable.
- Show evidence for every issue.
- Keep human-in-the-loop.
- Return structured JSON.
- Make demo endpoint stable.
- Prefer deterministic behavior for hackathon.
- Make code simple and readable.

### Do Not

- Do not auto-approve transactions.
- Do not auto-reject transactions.
- Do not claim final legal/compliance decision.
- Do not overbuild authentication for MVP.
- Do not depend fully on OCR.
- Do not depend fully on LLM output.
- Do not hide reasons behind a black-box score.

---

## Suggested Implementation Order

### Step 1 — Project setup

```bash
dotnet new webapi -n ComplianceDoc.Api
cd ComplianceDoc.Api
dotnet run
```

Make sure Swagger opens.

### Step 2 — Add domain models

Create enums and domain classes.

### Step 3 — Add extraction service

Start with regex/demo extraction.

### Step 4 — Add validation service

Implement rules:

- amount mismatch;
- currency mismatch;
- strange date;
- vague payment purpose;
- missing contract reference;
- missing invoice reference;
- service description mismatch.

### Step 5 — Add risk scoring service

Add additive score and level mapping.

### Step 6 — Add client message service

Generate deterministic explanation and draft message.

### Step 7 — Add controller

Implement:

```text
POST /api/compliance-checks
GET /api/compliance-checks/{id}
POST /api/compliance-checks/{id}/review
POST /api/compliance-checks/demo
```

### Step 8 — Test demo flow in Swagger

Use `/demo` first.

### Step 9 — Connect frontend

Frontend should call:

```text
POST /api/compliance-checks/demo
GET /api/compliance-checks/{id}
POST /api/compliance-checks/{id}/review
```

---

## Example Full Response Shape

```json
{
  "id": "6fa459ea-ee8a-3ca4-894e-db77e160355e",
  "caseNumber": "VC-2026-0142",
  "clientName": "GreenMarket Retail LLP",
  "createdAt": "2026-05-27T10:00:00Z",
  "contract": {
    "documentType": "Contract",
    "contractNumber": "GM-AT-05/2026",
    "documentDate": "2026-05-20",
    "sellerName": "AlmaTech Solutions LLP",
    "buyerName": "GreenMarket Retail LLP",
    "amount": 10000,
    "currency": "USD",
    "serviceDescription": "Development of internal CRM module."
  },
  "invoice": {
    "documentType": "Invoice",
    "invoiceNumber": "INV-2026-0147",
    "documentDate": "2026-05-27",
    "sellerName": "AlmaTech Solutions LLP",
    "buyerName": "GreenMarket Retail LLP",
    "amount": 10500,
    "currency": "USD",
    "serviceDescription": "IT consulting services."
  },
  "paymentOrder": {
    "documentType": "PaymentOrder",
    "documentDate": "2026-05-15",
    "payerName": "GreenMarket Retail LLP",
    "receiverName": "AlmaTech Solutions LLP",
    "amount": 10000,
    "currency": "USD",
    "paymentPurpose": "Payment"
  },
  "checklist": [
    {
      "code": "amount_match",
      "label": "Amounts match across documents",
      "passed": false,
      "details": "Contract/payment order amount differs from invoice amount."
    }
  ],
  "riskSummary": {
    "score": 95,
    "level": "Critical",
    "issues": [
      {
        "type": "AmountMismatch",
        "severity": "High",
        "scoreImpact": 35,
        "title": "Amount mismatch",
        "message": "The amount differs between contract, invoice and payment order.",
        "evidence": "Contract: 10000 USD; Invoice: 10500 USD; Payment: 10000 USD",
        "recommendedAction": "Request corrected invoice or additional agreement from the client."
      }
    ]
  },
  "explanation": "Overall risk level: Critical...",
  "draftClientMessage": "Hello! During the preliminary review...",
  "reviewedByHuman": false
}
```

---

## Pitch-Friendly Backend Description

Use this wording in the presentation:

```text
Our backend extracts structured data from contract, invoice and payment order, then runs an explainable validation engine. It compares amount, currency, dates, counterparties and payment purpose, calculates a transparent risk score and generates a draft client comment. The final decision is always made by a human compliance specialist.
```

---

## Final Reminder

This is a hackathon MVP. The best backend is not the most complex backend.

The best backend is the one that:

- works reliably during demo;
- clearly shows the business value;
- explains every detected issue;
- keeps the human in control;
- is simple enough to finish fast.
