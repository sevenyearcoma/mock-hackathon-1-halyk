import type { AssistantStatus, ComplianceCheck, RiskLevel } from "./types";

export const assistantStatusLabels: Record<AssistantStatus, string> = {
  NotStarted: "Не проверено",
  InProgress: "В работе",
  NeedsReview: "Требует проверки",
  Reviewed: "Проверено",
  Error: "Ошибка",
};

export function getAssistantStatusFromCheck(
  check?: ComplianceCheck | null,
): AssistantStatus {
  if (!check) return "NotStarted";
  if (check.reviewedByHuman) return "Reviewed";
  const issuesCount = check.riskSummary?.issues?.length ?? 0;
  if (issuesCount > 0) return "NeedsReview";
  return "Reviewed";
}

export function getAssistantStatusClass(status: AssistantStatus): string {
  switch (status) {
    case "NotStarted":
      return "border-slate-200 bg-slate-50 text-slate-600";
    case "InProgress":
      return "border-blue-200 bg-blue-50 text-blue-700";
    case "NeedsReview":
      return "border-amber-200 bg-amber-50 text-amber-700";
    case "Reviewed":
      return "border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Error":
      return "border-red-200 bg-red-50 text-red-700";
  }
}

export function getRiskLevelClass(level: RiskLevel): string {
  switch (level) {
    case "Low":
      return "border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Medium":
      return "border-blue-200 bg-blue-50 text-blue-700";
    case "High":
      return "border-amber-200 bg-amber-50 text-amber-700";
    case "Critical":
      return "border-red-200 bg-red-50 text-red-700";
  }
}
