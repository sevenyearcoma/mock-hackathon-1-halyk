import type { RiskLevel } from "@/lib/types";
import { getRiskLevelClass } from "@/lib/status";

const riskLevelLabels: Record<RiskLevel, string> = {
  Low: "Низкий",
  Medium: "Средний",
  High: "Высокий",
  Critical: "Критический",
};

export function RiskLevelBadge({ level }: { level?: RiskLevel | null }) {
  if (!level) return <span className="text-slate-400">—</span>;
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium ${getRiskLevelClass(level)}`}
    >
      {riskLevelLabels[level]}
    </span>
  );
}
