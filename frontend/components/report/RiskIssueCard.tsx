import type { RiskIssue, RiskSeverity } from "@/lib/types";
import { getRiskLevelClass } from "@/lib/status";

const severityLabels: Record<RiskSeverity, string> = {
  Low: "Низкий",
  Medium: "Средний",
  High: "Высокий",
  Critical: "Критический",
};

export function RiskIssueCard({ issue }: { issue: RiskIssue }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="flex items-start justify-between gap-3">
        <h3 className="min-w-0 text-sm font-semibold text-slate-800">{issue.title}</h3>
        <span
          className={`inline-flex shrink-0 items-center rounded-full border px-2 py-0.5 text-xs font-medium ${getRiskLevelClass(issue.severity)}`}
        >
          {severityLabels[issue.severity]}
        </span>
      </div>
      {issue.message && (
        <p className="mt-2 text-sm text-slate-600">{issue.message}</p>
      )}
      {issue.evidence && (
        <div className="mt-2 rounded bg-slate-50 px-3 py-2">
          <p className="break-words text-xs text-slate-500">
            <span className="font-medium text-slate-600">Основание: </span>
            {issue.evidence}
          </p>
        </div>
      )}
      {issue.recommendedAction && (
        <p className="mt-2 break-words text-xs text-slate-500">
          <span className="font-medium text-slate-600">Рекомендация: </span>
          {issue.recommendedAction}
        </p>
      )}
      <p className="mt-2 text-xs text-slate-400">Влияние на балл: +{issue.scoreImpact}</p>
    </div>
  );
}
