namespace ComplianceDoc.Api.Contracts.Requests;

public sealed class ReviewComplianceCheckRequest
{
    public bool ApprovedByHuman { get; set; }
    public string? HumanComment { get; set; }
    public string? EditedClientMessage { get; set; }
}
