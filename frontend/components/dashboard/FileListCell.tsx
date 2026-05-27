import { FileSpreadsheet, FileText } from "lucide-react";
import type { DashboardFile } from "@/lib/types";

export function FileListCell({ files }: { files: DashboardFile[] }) {
  return (
    <div className="flex flex-col gap-1">
      {files.map((file) => (
        <div key={file.name} className="flex items-center gap-1.5 text-xs text-slate-600">
          {file.type === "xlsx" ? (
            <FileSpreadsheet className="h-3.5 w-3.5 text-emerald-600 shrink-0" />
          ) : (
            <FileText className="h-3.5 w-3.5 text-blue-600 shrink-0" />
          )}
          <span className="truncate max-w-[140px]">{file.name}</span>
        </div>
      ))}
    </div>
  );
}
