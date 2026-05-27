namespace ComplianceDoc.Api.Domain.Enums;

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
    ServiceDescriptionMismatch,
    SanctionsListMatch
}
