"use client";

import { useState } from "react";
import { Copy, Check } from "lucide-react";

interface Props {
  draft?: string | null;
  onMessageChange: (msg: string) => void;
}

export function DraftClientMessagePanel({ draft, onMessageChange }: Props) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    if (!draft) return;
    await navigator.clipboard.writeText(draft);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">Черновик письма клиенту</h2>
        <button
          onClick={handleCopy}
          className="inline-flex items-center gap-1.5 rounded-md border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-600 transition-colors hover:bg-slate-50"
        >
          {copied ? (
            <><Check className="h-3.5 w-3.5 text-emerald-500" /> Скопировано</>
          ) : (
            <><Copy className="h-3.5 w-3.5" /> Копировать</>
          )}
        </button>
      </div>
      <div className="rounded-lg border border-slate-200 bg-white p-1">
        <textarea
          className="min-h-[200px] w-full resize-y rounded p-3 font-sans text-sm leading-relaxed text-slate-700 outline-none focus:ring-2 focus:ring-blue-200"
          value={draft ?? ""}
          onChange={(e) => onMessageChange(e.target.value)}
          placeholder="Черновик письма появится здесь после анализа ассистентом..."
        />
      </div>
      <p className="mt-1.5 text-xs text-slate-400">
        Отредактируйте при необходимости. Письмо не отправляется автоматически.
      </p>
    </div>
  );
}
