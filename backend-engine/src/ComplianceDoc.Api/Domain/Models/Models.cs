using ComplianceDoc.Api.Domain.Enums;

namespace ComplianceDoc.Api.Domain.Models;

public sealed class ExtractedDocumentFields
{
    public DocumentType DocumentType { get; set; }

    public string? DocumentNumber { get; set; }
    public DateOnly? DocumentDate { get; set; }
    public DateOnly? DueDate { get; set; }

    public string? ContractNumber { get; set; }
    public DateOnly? ContractDate { get; set; }
    public string? InvoiceNumber { get; set; }

    public string? SellerName { get; set; }
    public string? SellerBin { get; set; }
    public string? SellerBank { get; set; }
    public string? SellerIban { get; set; }

    public string? BuyerName { get; set; }
    public string? BuyerBin { get; set; }

    public string? PayerName { get; set; }
    public string? PayerBin { get; set; }
    public string? PayerBank { get; set; }
    public string? PayerIban { get; set; }

    public string? ReceiverName { get; set; }
    public string? ReceiverBin { get; set; }
    public string? ReceiverBank { get; set; }
    public string? ReceiverIban { get; set; }

    public decimal? Subtotal { get; set; }
    public decimal? Vat { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }

    public string? PaymentPurpose { get; set; }
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
    public int SourceId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

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
