"use client";

import { useState } from "react";
import { X, Upload } from "lucide-react";
import { createComplianceChecks } from "@/lib/api";
import type { ComplianceCaseInput, ComplianceCheck } from "@/lib/types";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";

interface Props {
  onSuccess: (checks: ComplianceCheck[]) => void;
  onClose: () => void;
}

export function JsonImportModal({ onSuccess, onClose }: Props) {
  const [json, setJson] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit() {
    setError(null);
    let parsed: ComplianceCaseInput[];
    try {
      const value = JSON.parse(json);
      parsed = Array.isArray(value) ? value : [value];
    } catch {
      setError("Неверный формат JSON. Ожидается массив объектов.");
      return;
    }

    setLoading(true);
    try {
      const checks = await createComplianceChecks(parsed);
      onSuccess(checks);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Ошибка запроса");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div className="w-full max-w-2xl rounded-xl border border-slate-200 bg-white shadow-xl">
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
          <h2 className="text-base font-semibold text-slate-800">Загрузить из JSON</h2>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600 transition-colors">
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="p-5">
          <p className="mb-3 text-xs text-slate-500">
            Вставьте один объект или массив JSON с полями{" "}
            <code className="rounded bg-slate-100 px-1 font-mono">invoice</code> и{" "}
            <code className="rounded bg-slate-100 px-1 font-mono">payment_order</code>.
          </p>
          <textarea
            className="h-64 w-full resize-none rounded-lg border border-slate-200 p-3 font-mono text-xs text-slate-700 outline-none focus:ring-2 focus:ring-blue-200"
            placeholder={'[{"id": 1, "invoice": {...}, "payment_order": {...}}]'}
            value={json}
            onChange={(e) => setJson(e.target.value)}
            spellCheck={false}
          />
          {error && <p className="mt-2 text-xs text-red-600">{error}</p>}
        </div>

        <div className="flex justify-end gap-3 border-t border-slate-200 px-5 py-4">
          <button
            onClick={onClose}
            className="rounded-md border border-slate-200 bg-white px-4 py-2 text-sm text-slate-600 hover:bg-slate-50 transition-colors"
          >
            Отмена
          </button>
          <button
            disabled={loading || !json.trim()}
            onClick={handleSubmit}
            className="inline-flex items-center gap-2 rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {loading ? <LoadingSpinner className="h-4 w-4" /> : <Upload className="h-4 w-4" />}
            Отправить
          </button>
        </div>
      </div>
    </div>
  );
}
