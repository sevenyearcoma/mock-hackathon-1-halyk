"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, AlertTriangle, ShieldCheck } from "lucide-react";
import { getComplianceCheck } from "@/lib/api";
import type { ComplianceCheck } from "@/lib/types";
import { getRiskLevelClass } from "@/lib/status";
import { ExtractedFieldsPanel } from "@/components/report/ExtractedFieldsPanel";
import { ChecklistPanel } from "@/components/report/ChecklistPanel";
import { RiskSummaryPanel } from "@/components/report/RiskSummaryPanel";
import { ExplanationPanel } from "@/components/report/ExplanationPanel";
import { DraftClientMessagePanel } from "@/components/report/DraftClientMessagePanel";
import { HumanReviewPanel } from "@/components/report/HumanReviewPanel";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";

const riskLevelLabels = { Low: "Низкий", Medium: "Средний", High: "Высокий", Critical: "Критический" } as const;

export default function ReportPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();

  const [check, setCheck] = useState<ComplianceCheck | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editedMessage, setEditedMessage] = useState("");

  useEffect(() => {
    getComplianceCheck(id)
      .then((c) => {
        setCheck(c);
        setEditedMessage(c.draftClientMessage ?? "");
      })
      .catch((e) => setError(e instanceof Error ? e.message : "Не удалось загрузить"))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <LoadingSpinner className="h-8 w-8 text-slate-400" />
      </div>
    );
  }

  if (error || !check) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <div className="text-center">
          <AlertTriangle className="mx-auto h-10 w-10 text-red-400" />
          <p className="mt-3 text-slate-600">{error ?? "Отчёт не найден"}</p>
          <button
            onClick={() => router.push("/compliance")}
            className="mt-4 text-sm text-blue-600 underline"
          >
            К дашборду
          </button>
        </div>
      </div>
    );
  }

  const issues = check.riskSummary?.issues ?? [];

  return (
    <main className="min-h-screen bg-slate-50">
      <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6">

        {/* Back */}
        <button
          onClick={() => router.push("/compliance")}
          className="mb-6 inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-slate-700"
        >
          <ArrowLeft className="h-4 w-4" />
          К дашборду
        </button>

        {/* Header */}
        <div className="mb-6">
          <div className="flex items-center gap-2">
            <ShieldCheck className="h-5 w-5 text-slate-600" />
            <h1 className="text-xl font-bold text-slate-900">Отчёт ИИ-ассистента</h1>
          </div>
          <p className="mt-0.5 text-sm text-slate-500">
            Предварительный анализ пакета документов валютного контроля
          </p>
          <p className="mt-1 text-xs text-slate-400">
            Дело {check.caseNumber} · {check.clientName}
          </p>
        </div>

        {/* Summary pills */}
        <div className="mb-6 flex flex-wrap items-center gap-3">
          <div className="rounded-full border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-700">
            Балл риска:{" "}
            <span className="font-semibold">{check.riskSummary.score}/100</span>
          </div>
          <span
            className={`inline-flex items-center rounded-full border px-3 py-1.5 text-sm font-medium ${getRiskLevelClass(check.riskSummary.level)}`}
          >
            {riskLevelLabels[check.riskSummary.level]}
          </span>
          <div className="rounded-full border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-700">
            Нарушений:{" "}
            <span className="font-semibold">{issues.length}</span>
          </div>
          <div
            className={`rounded-full border px-3 py-1.5 text-sm font-medium ${
              check.reviewedByHuman
                ? "border-emerald-200 bg-emerald-50 text-emerald-700"
                : "border-amber-200 bg-amber-50 text-amber-700"
            }`}
          >
            {check.reviewedByHuman ? "Проверено" : "Ожидает проверки"}
          </div>
        </div>

        {/* Disclaimer */}
        <div className="mb-6 flex items-start gap-2 rounded-lg border border-amber-100 bg-amber-50 px-4 py-3">
          <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-amber-500" />
          <p className="text-xs text-amber-700">
            Этот отчёт сформирован ИИ и должен быть проверен специалистом по комплаенс
            перед принятием каких-либо действий.
          </p>
        </div>

        {/* Two-column layout — min-w-0 prevents overflow from escaping the grid cell */}
        <div className="grid grid-cols-1 gap-8 lg:grid-cols-2">
          <div className="flex min-w-0 flex-col gap-8">
            <ExtractedFieldsPanel invoice={check.invoice} paymentOrder={check.paymentOrder} />
            <ChecklistPanel items={check.checklist} />
          </div>
          <div className="flex min-w-0 flex-col gap-8">
            <RiskSummaryPanel riskSummary={check.riskSummary} />
            <ExplanationPanel explanation={check.explanation} />
            <DraftClientMessagePanel
              draft={editedMessage}
              onMessageChange={setEditedMessage}
            />
            <HumanReviewPanel
              check={check}
              editedMessage={editedMessage}
              onReviewed={(updated) => setCheck(updated)}
            />
          </div>
        </div>
      </div>
    </main>
  );
}
