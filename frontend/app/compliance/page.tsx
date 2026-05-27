"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ShieldCheck, Info, RotateCcw, FileJson } from "lucide-react";
import { createDemoComplianceChecks } from "@/lib/api";
import { mockCases } from "@/lib/mockCases";
import type { ComplianceCheck, DashboardCase } from "@/lib/types";
import { getAssistantStatusFromCheck } from "@/lib/status";
import { loadCases, saveCases } from "@/lib/storage";
import { ComplianceCasesTable } from "@/components/dashboard/ComplianceCasesTable";
import { DashboardSummaryCards } from "@/components/dashboard/DashboardSummaryCards";
import { JsonImportModal } from "@/components/dashboard/JsonImportModal";

export default function CompliancePage() {
  const router = useRouter();

  // Hydrate from localStorage on first render; fall back to mockCases
  const [cases, setCases] = useState<DashboardCase[]>(() => loadCases() ?? mockCases);
  const [showImport, setShowImport] = useState(false);

  // Persist on every change
  useEffect(() => {
    saveCases(cases);
  }, [cases]);

  function update(updater: (prev: DashboardCase[]) => DashboardCase[]) {
    setCases((prev) => {
      const next = updater(prev);
      return next;
    });
  }

  async function runAssistant(sourceId: number) {
    update((prev) =>
      prev.map((item) =>
        item.sourceId === sourceId
          ? { ...item, assistantStatus: "InProgress", error: undefined }
          : item,
      ),
    );

    try {
      const checks = await createDemoComplianceChecks();
      const check = checks.find((c) => (c.riskSummary?.issues?.length ?? 0) > 0) ?? checks[0];

      update((prev) =>
        prev.map((item) =>
          item.sourceId === sourceId
            ? { ...item, check, checkId: check.id, assistantStatus: getAssistantStatusFromCheck(check) }
            : item,
        ),
      );

      router.push(`/compliance/${check.id}`);
    } catch (e) {
      update((prev) =>
        prev.map((item) =>
          item.sourceId === sourceId
            ? { ...item, assistantStatus: "Error", error: e instanceof Error ? e.message : "Ошибка" }
            : item,
        ),
      );
    }
  }

  function handleJsonSuccess(checks: ComplianceCheck[]) {
    setShowImport(false);
    // Add new rows for each returned check
    const newCases: DashboardCase[] = checks.map((check) => ({
      sourceId: check.sourceId,
      caseNumber: check.caseNumber ?? `CASE-${check.sourceId}`,
      clientName: check.clientName ?? "—",
      receivedAt: check.createdAt,
      files: [],
      amount: check.invoice?.amount ?? 0,
      currency: check.invoice?.currency ?? "",
      baseStatus: "На проверке",
      assistantStatus: getAssistantStatusFromCheck(check),
      checkId: check.id,
      check,
    }));

    update((prev) => {
      // Replace rows with same sourceId, otherwise append
      const existing = new Set(newCases.map((c) => c.sourceId));
      const filtered = prev.filter((c) => !existing.has(c.sourceId));
      return [...filtered, ...newCases];
    });

    if (checks.length === 1) {
      router.push(`/compliance/${checks[0].id}`);
    }
  }

  function resetDashboard() {
    setCases(mockCases);
  }

  return (
    <main className="min-h-screen bg-slate-50">
      <div className="mx-auto max-w-[1400px] px-4 py-8 sm:px-6">

        {/* Header */}
        <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-2">
              <ShieldCheck className="h-6 w-6 text-slate-700" />
              <h1 className="text-xl font-bold text-slate-900">Панель валютного контроля</h1>
            </div>
            <p className="mt-1 text-sm text-slate-500">
              Очередь комплаенс-проверок с поддержкой ИИ
            </p>
          </div>

          <div className="flex items-center gap-2">
            {/* JSON import button */}
            <button
              onClick={() => setShowImport(true)}
              className="inline-flex items-center gap-1.5 rounded-md border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 transition-colors"
            >
              <FileJson className="h-4 w-4" />
              Загрузить из JSON
            </button>

            {/* Reset */}
            <button
              onClick={resetDashboard}
              title="Сбросить дашборд"
              className="rounded-md border border-slate-200 bg-white p-2 text-slate-500 hover:bg-slate-50 hover:text-slate-700 transition-colors"
            >
              <RotateCcw className="h-4 w-4" />
            </button>

            {/* Disclaimer chip */}
            <div className="hidden sm:flex items-start gap-1.5 rounded-lg border border-blue-100 bg-blue-50 px-3 py-2 max-w-xs">
              <Info className="mt-0.5 h-4 w-4 shrink-0 text-blue-500" />
              <p className="text-xs text-blue-700">
                Вывод ИИ предварительный. Решение принимает специалист.
              </p>
            </div>
          </div>
        </div>

        {/* Summary cards */}
        <div className="mb-6">
          <DashboardSummaryCards cases={cases} />
        </div>

        <p className="mb-3 text-xs text-slate-400">
          Этот дашборд симулирует существующую очередь проверок. ИИ-ассистент добавляет
          анализ документов, оценку рисков и отчёт поверх текущего рабочего процесса.
        </p>

        <ComplianceCasesTable cases={cases} onRunAssistant={runAssistant} />
      </div>

      {showImport && (
        <JsonImportModal
          onSuccess={handleJsonSuccess}
          onClose={() => setShowImport(false)}
        />
      )}
    </main>
  );
}
