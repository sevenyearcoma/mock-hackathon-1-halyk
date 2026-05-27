"use client";

import Link from "next/link";
import { Loader2, Play, ExternalLink, RotateCcw } from "lucide-react";
import type { DashboardCase } from "@/lib/types";
import { AssistantStatusBadge } from "./AssistantStatusBadge";
import { RiskLevelBadge } from "./RiskLevelBadge";
import { FileListCell } from "./FileListCell";
import { formatMoney, formatDateTime } from "@/lib/formatters";

interface Props {
  cases: DashboardCase[];
  onRunAssistant: (sourceId: number) => void;
}

export function ComplianceCasesTable({ cases, onRunAssistant }: Props) {
  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50">
            <th
              colSpan={6}
              className="border-r border-slate-200 px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500"
            >
              Существующая система
            </th>
            <th
              colSpan={5}
              className="bg-blue-50 px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-blue-600"
            >
              ✦ Слой ИИ-ассистента
            </th>
          </tr>
          <tr className="border-b border-slate-200 text-left text-xs font-medium text-slate-600">
            <th className="whitespace-nowrap px-4 py-3">Номер дела</th>
            <th className="whitespace-nowrap px-4 py-3">Клиент</th>
            <th className="whitespace-nowrap px-4 py-3">Файлы</th>
            <th className="whitespace-nowrap px-4 py-3">Сумма</th>
            <th className="whitespace-nowrap px-4 py-3">Поступило</th>
            <th className="whitespace-nowrap border-r border-slate-200 px-4 py-3">Статус</th>
            <th className="whitespace-nowrap bg-blue-50/50 px-4 py-3">Статус ассистента</th>
            <th className="whitespace-nowrap bg-blue-50/50 px-4 py-3">Балл риска</th>
            <th className="whitespace-nowrap bg-blue-50/50 px-4 py-3">Уровень риска</th>
            <th className="whitespace-nowrap bg-blue-50/50 px-4 py-3">Нарушения</th>
            <th className="whitespace-nowrap bg-blue-50/50 px-4 py-3">Действия</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {cases.map((c) => (
            <tr key={c.sourceId} className="transition-colors hover:bg-slate-50">
              <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-700">
                {c.caseNumber}
              </td>
              <td className="whitespace-nowrap px-4 py-3 font-medium text-slate-800">
                {c.clientName}
              </td>
              <td className="px-4 py-3">
                <FileListCell files={c.files} />
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-slate-700">
                {formatMoney(c.amount, c.currency)}
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-xs text-slate-500">
                {formatDateTime(c.receivedAt)}
              </td>
              <td className="border-r border-slate-200 px-4 py-3">
                <span className="inline-flex items-center rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-xs font-medium text-slate-600">
                  {c.baseStatus}
                </span>
              </td>

              <td className="bg-blue-50/30 px-4 py-3">
                <AssistantStatusBadge status={c.assistantStatus} />
              </td>
              <td className="bg-blue-50/30 px-4 py-3 font-medium text-slate-700">
                {c.check ? (
                  <span className="tabular-nums">
                    {c.check.riskSummary.score}
                    <span className="font-normal text-slate-400">/100</span>
                  </span>
                ) : (
                  <span className="text-slate-300">—</span>
                )}
              </td>
              <td className="bg-blue-50/30 px-4 py-3">
                <RiskLevelBadge level={c.check?.riskSummary?.level} />
              </td>
              <td className="bg-blue-50/30 px-4 py-3">
                {c.check ? (
                  <span className="inline-flex items-center justify-center rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
                    {c.check.riskSummary.issues?.length ?? 0}
                  </span>
                ) : (
                  <span className="text-slate-300">—</span>
                )}
              </td>
              <td className="bg-blue-50/30 px-4 py-3">
                <ActionCell c={c} onRun={onRunAssistant} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function ActionCell({ c, onRun }: { c: DashboardCase; onRun: (id: number) => void }) {
  if (c.assistantStatus === "InProgress") {
    return (
      <span className="inline-flex items-center gap-1.5 text-xs text-blue-600">
        <Loader2 className="h-3.5 w-3.5 animate-spin" />
        Проверяем...
      </span>
    );
  }

  if (c.assistantStatus === "Error") {
    return (
      <div className="flex flex-col gap-1">
        <span className="text-xs text-red-600">Ошибка проверки</span>
        <button
          onClick={() => onRun(c.sourceId)}
          className="inline-flex items-center gap-1 text-xs text-slate-500 underline hover:text-slate-700"
        >
          <RotateCcw className="h-3 w-3" />
          Повторить
        </button>
      </div>
    );
  }

  if (c.checkId) {
    return (
      <Link
        href={`/compliance/${c.checkId}`}
        className="inline-flex items-center gap-1.5 rounded-md border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 transition-colors hover:border-slate-300 hover:bg-slate-50"
      >
        <ExternalLink className="h-3 w-3" />
        Посмотреть отчёт
      </Link>
    );
  }

  return (
    <button
      onClick={() => onRun(c.sourceId)}
      className="inline-flex items-center gap-1.5 rounded-md bg-blue-600 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-blue-700"
    >
      <Play className="h-3 w-3" />
      Запустить ИИ
    </button>
  );
}
