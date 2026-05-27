using System.Text;
using ComplianceDoc.Api.Domain.Models;
using ComplianceDoc.Api.Services.Interfaces;

namespace ComplianceDoc.Api.Services.Implementations;

public sealed class ClientMessageService : IClientMessageService
{
    public string GenerateExplanation(RiskSummary riskSummary)
    {
        if (riskSummary.Issues.Count == 0)
            return "No major inconsistencies were detected. The documents can proceed to standard human review.";

        var lines = new List<string>
        {
            $"Overall risk level: {riskSummary.Level}.",
            $"Risk score: {riskSummary.Score}/100.",
            "",
            "Detected issues:"
        };

        foreach (var issue in riskSummary.Issues)
            lines.Add($"- {issue.Title}: {issue.Message} Recommended action: {issue.RecommendedAction}");

        lines.Add("");
        lines.Add("This is not a final compliance decision. A bank employee must review the result before taking action.");

        return string.Join(Environment.NewLine, lines);
    }

    public string GenerateClientMessage(RiskSummary riskSummary)
    {
        var issues = riskSummary.Issues;

        if (issues.Count == 0)
        {
            return "Hello!\n\nWe have completed the preliminary document review and did not identify major inconsistencies.\nThe documents will proceed to standard compliance review.\n\nThank you.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Hello!");
        sb.AppendLine();
        sb.AppendLine("During the preliminary review of the documents for the foreign currency payment, we identified the following points that require clarification:");
        sb.AppendLine();

        for (var i = 0; i < issues.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {issues[i].Message}");
            sb.AppendLine($"   Required action: {issues[i].RecommendedAction}");
        }

        sb.AppendLine();
        sb.AppendLine("Please provide corrected documents or additional clarification so that the review can continue.");
        sb.AppendLine();
        sb.AppendLine("Thank you!");

        return sb.ToString();
    }
}
