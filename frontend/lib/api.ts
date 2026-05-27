import type { ComplianceCaseInput, ComplianceCheck, ReviewComplianceCheckRequest } from "./types";

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5123";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
    cache: "no-store",
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `Request failed with status ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export function getComplianceChecks(): Promise<ComplianceCheck[]> {
  return request<ComplianceCheck[]>("/api/compliance-checks");
}

export function getComplianceCheck(id: string): Promise<ComplianceCheck> {
  return request<ComplianceCheck>(`/api/compliance-checks/${id}`);
}

export function createComplianceChecks(
  cases: ComplianceCaseInput[],
): Promise<ComplianceCheck[]> {
  return request<ComplianceCheck[]>("/api/compliance-checks", {
    method: "POST",
    body: JSON.stringify(cases),
  });
}

export function createDemoComplianceChecks(): Promise<ComplianceCheck[]> {
  return request<ComplianceCheck[]>("/api/compliance-checks/demo", {
    method: "POST",
  });
}

export function reviewComplianceCheck(
  id: string,
  body: ReviewComplianceCheckRequest,
): Promise<ComplianceCheck> {
  return request<ComplianceCheck>(`/api/compliance-checks/${id}/review`, {
    method: "POST",
    body: JSON.stringify(body),
  });
}
