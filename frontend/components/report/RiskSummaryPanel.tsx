import type { RiskLevel, RiskSummary } from "@/lib/types";
import { getRiskLevelClass } from "@/lib/status";
import { RiskIssueCard } from "./RiskIssueCard";

const riskLevelLabels: Record<RiskLevel, string> = {
  Low: "Низкий",
  Medium: "Средний",
  High: "Высокий",
  Critical: "Критический",
};

export function RiskSummaryPanel({ riskSummary }: { riskSummary: RiskSummary }) {
  const issues = riskSummary.issues ?? [];

  return (
    <div>
      <h2 className="mb-3 text-base font-semibold text-slate-800">Сводка по рискам</h2>

      <div className="mb-4 flex flex-wrap gap-3">
        <div className="rounded-lg border border-slate-200 bg-white px-4 py-3">
          <p className="text-xs text-slate-500">Балл риска</p>
          <p className="mt-0.5 tabular-nums text-2xl font-semibold text-slate-800">
            {riskSummary.score}
            <span className="text-sm font-normal text-slate-400">/100</span>
          </p>
        </div>
        <div className="rounded-lg border border-slate-200 bg-white px-4 py-3">
          <p className="text-xs text-slate-500">Уровень риска</p>
          <div className="mt-1">
            <span
              className={`inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium ${getRiskLevelClass(riskSummary.level)}`}
            >
              {riskLevelLabels[riskSummary.level]}
            </span>
          </div>
        </div>
        <div className="rounded-lg border border-slate-200 bg-white px-4 py-3">
          <p className="text-xs text-slate-500">Выявлено нарушений</p>
          <p className="mt-0.5 text-2xl font-semibold text-slate-800">{issues.length}</p>
        </div>
      </div>

      {issues.length === 0 ? (
        <p className="text-sm text-emerald-600">
          Нарушений не обнаружено. Документы согласованы.
        </p>
      ) : (
        <div className="flex flex-col gap-3">
          {issues.map((issue, i) => (
            <RiskIssueCard key={i} issue={issue} />
          ))}
        </div>
      )}
    </div>
  );
}
