export function ExplanationPanel({ explanation }: { explanation?: string | null }) {
  if (!explanation) return null;

  return (
    <div>
      <h2 className="mb-3 text-base font-semibold text-slate-800">Пояснение ИИ</h2>
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <pre className="whitespace-pre-wrap break-words font-sans text-sm leading-relaxed text-slate-700">
          {explanation}
        </pre>
      </div>
    </div>
  );
}
