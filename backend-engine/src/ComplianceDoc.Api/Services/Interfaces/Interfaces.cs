using ComplianceDoc.Api.Contracts.Requests;
using ComplianceDoc.Api.Domain.Models;

namespace ComplianceDoc.Api.Services.Interfaces;

public interface IComplianceValidationService
{
    ComplianceCheck CreateCheck(ComplianceCaseInput input);
}

public interface IRiskScoringService
{
    RiskSummary Calculate(List<RiskIssue> issues);
}

public interface IClientMessageService
{
    string GenerateExplanation(RiskSummary riskSummary);
    string GenerateClientMessage(RiskSummary riskSummary);
}

public interface IComplianceCheckStore
{
    void Save(ComplianceCheck check);
    ComplianceCheck? GetById(Guid id);
    IReadOnlyList<ComplianceCheck> GetAll();
}
