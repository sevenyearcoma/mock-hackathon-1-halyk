import { CheckCircle2, AlertTriangle } from "lucide-react";
import type { ChecklistItem } from "@/lib/types";

export function ChecklistPanel({ items }: { items?: ChecklistItem[] | null }) {
  if (!items || items.length === 0) {
    return (
      <div>
        <h2 className="mb-3 text-base font-semibold text-slate-800">Чек-лист</h2>
        <p className="text-sm text-slate-400">Ассистент не вернул пункты чек-листа.</p>
      </div>
    );
  }

  return (
    <div>
      <h2 className="mb-3 text-base font-semibold text-slate-800">Чек-лист</h2>
      <div className="divide-y divide-slate-100 rounded-lg border border-slate-200 bg-white">
        {items.map((item, i) => (
          <div key={item.code ?? i} className="flex items-start gap-3 px-4 py-3">
            {item.passed ? (
              <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-emerald-500" />
            ) : (
              <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-amber-500" />
            )}
            <div className="min-w-0">
              <p className="text-sm text-slate-700">{item.label}</p>
              {item.details && (
                <p className="mt-0.5 break-words text-xs text-slate-500">{item.details}</p>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
