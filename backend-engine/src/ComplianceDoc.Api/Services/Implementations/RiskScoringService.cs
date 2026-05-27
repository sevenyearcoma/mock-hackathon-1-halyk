using ComplianceDoc.Api.Domain.Enums;
using ComplianceDoc.Api.Domain.Models;
using ComplianceDoc.Api.Services.Interfaces;

namespace ComplianceDoc.Api.Services.Implementations;

public sealed class RiskScoringService : IRiskScoringService
{
    public RiskSummary Calculate(List<RiskIssue> issues)
    {
        var score = Math.Min(issues.Sum(x => x.ScoreImpact), 100);
        return new RiskSummary
        {
            Score = score,
            Level = ToRiskLevel(score),
            Issues = issues
        };
    }

    private static RiskLevel ToRiskLevel(int score) => score switch
    {
        <= 20 => RiskLevel.Low,
        <= 50 => RiskLevel.Medium,
        <= 80 => RiskLevel.High,
        _ => RiskLevel.Critical
    };
}
