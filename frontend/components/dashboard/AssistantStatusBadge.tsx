import type { AssistantStatus } from "@/lib/types";
import { assistantStatusLabels, getAssistantStatusClass } from "@/lib/status";

export function AssistantStatusBadge({ status }: { status: AssistantStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium ${getAssistantStatusClass(status)}`}
    >
      {assistantStatusLabels[status]}
    </span>
  );
}
