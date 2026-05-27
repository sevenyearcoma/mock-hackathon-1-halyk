import type { DashboardCase } from "./types";

const KEY = "compliance_cases_v1";

export function loadCases(): DashboardCase[] | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = localStorage.getItem(KEY);
    return raw ? (JSON.parse(raw) as DashboardCase[]) : null;
  } catch {
    return null;
  }
}

export function saveCases(cases: DashboardCase[]): void {
  if (typeof window === "undefined") return;
  try {
    localStorage.setItem(KEY, JSON.stringify(cases));
  } catch {}
}

export function clearCases(): void {
  if (typeof window === "undefined") return;
  try {
    localStorage.removeItem(KEY);
  } catch {}
}
