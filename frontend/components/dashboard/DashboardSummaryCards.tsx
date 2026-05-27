import type { DashboardCase } from "@/lib/types";

export function DashboardSummaryCards({ cases }: { cases: DashboardCase[] }) {
  const total = cases.length;
  const inProgress = cases.filter((c) => c.assistantStatus === "InProgress").length;
  const needsReview = cases.filter((c) => c.assistantStatus === "NeedsReview").length;
  const highRisk = cases.filter(
    (c) =>
      c.check?.riskSummary?.level === "High" ||
      c.check?.riskSummary?.level === "Critical",
  ).length;

  const cards = [
    { label: "Входящих дел", value: total, color: "text-slate-700" },
    { label: "На проверке ИИ", value: inProgress, color: "text-blue-700" },
    { label: "Требует проверки", value: needsReview, color: "text-amber-700" },
    { label: "Высокий риск", value: highRisk, color: "text-red-700" },
  ];

  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
      {cards.map((card) => (
        <div key={card.label} className="rounded-lg border border-slate-200 bg-white px-4 py-3">
          <p className="text-xs text-slate-500">{card.label}</p>
          <p className={`mt-1 text-2xl font-semibold ${card.color}`}>{card.value}</p>
        </div>
      ))}
    </div>
  );
}
