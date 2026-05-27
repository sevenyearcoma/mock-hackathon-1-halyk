"use client";

import { useState } from "react";
import { CheckCircle2 } from "lucide-react";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import type { ComplianceCheck } from "@/lib/types";
import { reviewComplianceCheck } from "@/lib/api";

interface Props {
  check: ComplianceCheck;
  editedMessage: string;
  onReviewed: (updated: ComplianceCheck) => void;
}

export function HumanReviewPanel({ check, editedMessage, onReviewed }: Props) {
  const [comment, setComment] = useState(check.humanComment ?? "");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (check.reviewedByHuman) {
    return (
      <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4">
        <div className="flex items-center gap-2">
          <CheckCircle2 className="h-5 w-5 text-emerald-600" />
          <p className="text-sm font-semibold text-emerald-700">
            Отчёт проверен специалистом комплаенс
          </p>
        </div>
        {check.humanComment && (
          <p className="mt-2 text-sm text-emerald-600">Комментарий: {check.humanComment}</p>
        )}
      </div>
    );
  }

  async function handleReview() {
    setLoading(true);
    setError(null);
    try {
      const updated = await reviewComplianceCheck(check.id, {
        approvedByHuman: true,
        humanComment: comment || undefined,
        editedClientMessage: editedMessage || undefined,
      });
      onReviewed(updated);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Ошибка запроса");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div>
      <h2 className="mb-3 text-base font-semibold text-slate-800">Проверка специалистом</h2>
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <p className="mb-3 text-xs text-slate-500">
          Отметка отчёта как проверенного{" "}
          <strong>не означает</strong> одобрение или отклонение платежа. Это фиксирует,
          что специалист по комплаенс ознакомился с результатом ИИ.
        </p>
        <label className="mb-1 block text-xs font-medium text-slate-600">
          Комментарий специалиста (необязательно)
        </label>
        <textarea
          className="w-full resize-none rounded-md border border-slate-200 p-2.5 text-sm text-slate-700 outline-none focus:ring-2 focus:ring-blue-200"
          rows={3}
          placeholder="Например: Проверено. Клиенту направлен запрос на уточнение."
          value={comment}
          onChange={(e) => setComment(e.target.value)}
        />
        {error && <p className="mt-2 text-xs text-red-600">{error}</p>}
        <button
          disabled={loading}
          onClick={handleReview}
          className="mt-3 inline-flex items-center gap-2 rounded-md bg-slate-800 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-900 disabled:opacity-50"
        >
          {loading && <LoadingSpinner className="h-4 w-4" />}
          Отметить отчёт как проверенный
        </button>
      </div>
    </div>
  );
}
