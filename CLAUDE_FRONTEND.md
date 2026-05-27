# CLAUDE.md — Frontend Guide for ComplianceDoc Copilot (Next.js)

## 0. Project summary

We are building a hackathon MVP frontend for **ComplianceDoc Copilot**.

The product is not a replacement for the bank compliance system. It should look like an **AI add-on inside an existing compliance dashboard**.

The story:

1. Compliance already has an internal dashboard where incoming document packages arrive.
2. Each package contains financial documents such as PDF invoices and XLSX payment orders.
3. Our AI assistant adds a new layer to this dashboard.
4. The most visible addition is a new column called `assistantStatus`.
5. The dashboard also shows AI output preview: risk score, risk level, number of detected issues.
6. The user can click **View Report / Посмотреть отчет** and open a new screen with the full assistant report.
7. The compliance specialist reviews the result, edits the draft client message, and marks the case as reviewed.

Important product rule:

```text
AI does not make the final compliance/legal decision.
The final decision always stays with the compliance specialist.
```

---

## 1. Tech stack

Use:

```text
Next.js 14+ or 15+
App Router
TypeScript
Tailwind CSS
shadcn/ui style components or simple custom components
lucide-react for icons
```

Recommended install:

```bash
npx create-next-app@latest compliance-doc-frontend --typescript --tailwind --eslint --app
cd compliance-doc-frontend
npm install lucide-react clsx tailwind-merge
```

Optional, only if needed:

```bash
npm install @tanstack/react-table
```

For this hackathon MVP, a simple custom table is enough. Do not over-engineer.

---

## 2. Backend API contract

The frontend integrates with the provided .NET backend API.

Set base URL with environment variable:

```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

Available endpoints:

```http
GET  /api/compliance-checks
POST /api/compliance-checks
GET  /api/compliance-checks/{id}
POST /api/compliance-checks/{id}/review
POST /api/compliance-checks/demo
```

Backend behavior:

- `GET /api/compliance-checks` returns `ComplianceCheck[]`.
- `POST /api/compliance-checks` accepts `ComplianceCaseInput[]` and returns `ComplianceCheck[]`.
- `POST /api/compliance-checks/demo` returns demo `ComplianceCheck[]`.
- `GET /api/compliance-checks/{id}` returns one `ComplianceCheck`.
- `POST /api/compliance-checks/{id}/review` stores human review result.

Important backend schema detail:

The current API has `invoice` and `paymentOrder` fields in `ComplianceCheck`. It does not currently expose a separate `contract` object. Do not assume contract exists unless backend is updated.

---

## 3. Product framing

The UI must feel like an internal bank operations dashboard, not a landing page.

The user should understand this:

```text
Before: compliance specialist manually opens PDFs/XLSX files and checks fields.
After: the same dashboard shows AI assistant status, risk score, and a report.
```

Main value:

```text
The assistant reduces routine manual comparison work and helps the specialist focus on risk judgment.
```

Do not show dangerous buttons like:

```text
Approve payment
Reject payment
Block transaction
```

Use safer buttons:

```text
Run Assistant
Посмотреть отчет
Mark as reviewed
Save comment
Copy draft
Request clarification
```

---

## 4. Naming decision: assistant status column

Use internal field name:

```ts
assistantStatus
```

Use UI column label:

```text
AI Status
```

or Russian UI:

```text
Статус ассистента
```

Avoid snake_case in UI. `assistant_status` is okay for database/API naming, but in TypeScript use camelCase.

Recommended statuses:

```ts
export type AssistantStatus =
  | "NotStarted"
  | "InProgress"
  | "NeedsReview"
  | "Reviewed"
  | "Error";
```

Labels:

```ts
export const assistantStatusLabels: Record<AssistantStatus, string> = {
  NotStarted: "Не проверено",
  InProgress: "В работе",
  NeedsReview: "Требует проверки",
  Reviewed: "Проверено",
  Error: "Ошибка",
};
```

Meaning:

| Status | UI label | Meaning |
|---|---|---|
| `NotStarted` | Не проверено | Assistant has not analyzed the case yet |
| `InProgress` | В работе | Assistant is analyzing the documents |
| `NeedsReview` | Требует проверки | Assistant found issues; human review needed |
| `Reviewed` | Проверено | Human reviewed the report |
| `Error` | Ошибка | API or processing failed |

For demo, the important transition is:

```text
Не проверено -> В работе -> Требует проверки -> Проверено
```

---

## 5. Main user flow

```text
1. User opens /compliance
2. User sees default compliance dashboard with incoming PDF/XLSX packages
3. User sees new AI columns: Статус ассистента, Risk Score, Risk Level, Issues
4. User clicks Run Assistant
5. Row status changes to В работе
6. Frontend calls POST /api/compliance-checks/demo or POST /api/compliance-checks
7. Row updates with risk score and risk level
8. Button Посмотреть отчет appears
9. User opens /compliance/[id]
10. User reviews extracted fields, checklist, risks, explanation, draft message
11. User edits draft client message
12. User clicks Mark as reviewed
13. Frontend calls POST /api/compliance-checks/{id}/review
14. Status becomes Проверено
```

---

## 6. Routes

Use Next.js App Router.

### `/`

Redirect to:

```text
/compliance
```

### `/compliance`

Main dashboard page.

This page simulates the existing compliance dashboard.

### `/compliance/[id]`

AI assistant report page.

This is the new screen where the user sees the full report.

---

## 7. Recommended folder structure

```text
src/
  app/
    page.tsx
    compliance/
      page.tsx
      [id]/
        page.tsx

  components/
    dashboard/
      ComplianceDashboard.tsx
      ComplianceCasesTable.tsx
      AssistantStatusBadge.tsx
      RiskLevelBadge.tsx
      FileListCell.tsx
      RunAssistantButton.tsx
      DashboardSummaryCards.tsx

    report/
      ReportHeader.tsx
      ExtractedFieldsPanel.tsx
      ChecklistPanel.tsx
      RiskSummaryPanel.tsx
      RiskIssueCard.tsx
      ExplanationPanel.tsx
      DraftClientMessagePanel.tsx
      HumanReviewPanel.tsx

    ui/
      Badge.tsx
      Button.tsx
      Card.tsx
      Textarea.tsx
      LoadingSpinner.tsx

  lib/
    api.ts
    types.ts
    formatters.ts
    status.ts
    mockCases.ts
    storage.ts
```

---

## 8. TypeScript API types

Create `src/lib/types.ts`.

```ts
export type RiskLevel = "Low" | "Medium" | "High" | "Critical";
export type RiskSeverity = "Low" | "Medium" | "High" | "Critical";

export type IssueType =
  | "AmountMismatch"
  | "CurrencyMismatch"
  | "CounterpartyMismatch"
  | "BankDetailsMismatch"
  | "VaguePaymentPurpose"
  | "StrangeDate"
  | "MissingRequiredField"
  | "MissingContractReference"
  | "MissingInvoiceReference"
  | "ServiceDescriptionMismatch";

export type DocumentType = "Contract" | "Invoice" | "PaymentOrder";

export type AssistantStatus =
  | "NotStarted"
  | "InProgress"
  | "NeedsReview"
  | "Reviewed"
  | "Error";

export type ChecklistItem = {
  code?: string | null;
  label?: string | null;
  passed: boolean;
  details?: string | null;
};

export type ExtractedDocumentFields = {
  documentType: DocumentType;
  documentNumber?: string | null;
  documentDate?: string | null;
  dueDate?: string | null;
  contractNumber?: string | null;
  contractDate?: string | null;
  invoiceNumber?: string | null;

  sellerName?: string | null;
  sellerBin?: string | null;
  sellerBank?: string | null;
  sellerIban?: string | null;

  buyerName?: string | null;
  buyerBin?: string | null;

  payerName?: string | null;
  payerBin?: string | null;
  payerBank?: string | null;
  payerIban?: string | null;

  receiverName?: string | null;
  receiverBin?: string | null;
  receiverBank?: string | null;
  receiverIban?: string | null;

  subtotal?: number | null;
  vat?: number | null;
  amount?: number | null;
  currency?: string | null;
  paymentPurpose?: string | null;
};

export type RiskIssue = {
  type: IssueType;
  severity: RiskSeverity;
  scoreImpact: number;
  title?: string | null;
  message?: string | null;
  evidence?: string | null;
  recommendedAction?: string | null;
};

export type RiskSummary = {
  score: number;
  level: RiskLevel;
  issues?: RiskIssue[] | null;
};

export type ComplianceCheck = {
  id: string;
  sourceId: number;
  caseNumber?: string | null;
  clientName?: string | null;
  createdAt: string;
  invoice: ExtractedDocumentFields;
  paymentOrder: ExtractedDocumentFields;
  checklist?: ChecklistItem[] | null;
  riskSummary: RiskSummary;
  explanation?: string | null;
  draftClientMessage?: string | null;
  reviewedByHuman: boolean;
  humanComment?: string | null;
  finalClientMessage?: string | null;
};

export type InvoiceInput = {
  inv_number?: string | null;
  inv_date?: string | null;
  inv_due_date?: string | null;
  inv_currency?: string | null;
  inv_subtotal?: number | null;
  inv_vat?: number | null;
  inv_total?: number | null;
  inv_seller_name?: string | null;
  inv_seller_bin?: string | null;
  inv_seller_bank?: string | null;
  inv_seller_iban?: string | null;
  inv_buyer_name?: string | null;
  inv_buyer_bin?: string | null;
  inv_contract_no?: string | null;
  inv_contract_date?: string | null;
};

export type PaymentOrderInput = {
  pay_date?: string | null;
  pay_payer_name?: string | null;
  pay_payer_bin?: string | null;
  pay_payer_bank?: string | null;
  pay_payer_iban?: string | null;
  pay_receiver_name?: string | null;
  pay_receiver_bin?: string | null;
  pay_receiver_bank?: string | null;
  pay_receiver_iban?: string | null;
  pay_amount?: number | null;
  pay_currency?: string | null;
  pay_purpose?: string | null;
};

export type ComplianceCaseInput = {
  id: number;
  invoice: InvoiceInput;
  payment_order: PaymentOrderInput;
};

export type ReviewComplianceCheckRequest = {
  approvedByHuman: boolean;
  humanComment?: string | null;
  editedClientMessage?: string | null;
};

export type DashboardCase = {
  sourceId: number;
  caseNumber: string;
  clientName: string;
  receivedAt: string;
  files: DashboardFile[];
  amount: number;
  currency: string;
  baseStatus: "На проверке" | "Ожидает документы" | "Возвращено клиенту" | "Новый";
  assistantStatus: AssistantStatus;
  checkId?: string;
  check?: ComplianceCheck;
};

export type DashboardFile = {
  name: string;
  type: "pdf" | "xlsx" | "docx" | "unknown";
};
```

---

## 9. API client

Create `src/lib/api.ts`.

```ts
import type {
  ComplianceCaseInput,
  ComplianceCheck,
  ReviewComplianceCheckRequest,
} from "./types";

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

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
```

---

## 10. Mock existing compliance dashboard data

Create `src/lib/mockCases.ts`.

The mock data represents the bank dashboard before our AI assistant runs.

```ts
import type { DashboardCase } from "./types";

export const mockCases: DashboardCase[] = [
  {
    sourceId: 101,
    caseNumber: "VC-2026-0142",
    clientName: "GreenMarket Retail LLP",
    receivedAt: "2026-05-27T10:42:00Z",
    files: [
      { name: "invoice_0147.pdf", type: "pdf" },
      { name: "payment_order_0142.xlsx", type: "xlsx" },
    ],
    amount: 10000,
    currency: "USD",
    baseStatus: "На проверке",
    assistantStatus: "NotStarted",
  },
  {
    sourceId: 102,
    caseNumber: "VC-2026-0143",
    clientName: "Nomad Trade TOO",
    receivedAt: "2026-05-27T11:05:00Z",
    files: [
      { name: "invoice_2001.pdf", type: "pdf" },
      { name: "payment_order_2001.xlsx", type: "xlsx" },
    ],
    amount: 24500,
    currency: "EUR",
    baseStatus: "Новый",
    assistantStatus: "NotStarted",
  },
  {
    sourceId: 103,
    caseNumber: "VC-2026-0144",
    clientName: "Alatau Logistics LLP",
    receivedAt: "2026-05-27T11:22:00Z",
    files: [
      { name: "invoice_882.pdf", type: "pdf" },
      { name: "payment.xlsx", type: "xlsx" },
    ],
    amount: 7300000,
    currency: "KZT",
    baseStatus: "Ожидает документы",
    assistantStatus: "NotStarted",
  },
];
```

---

## 11. Status helpers

Create `src/lib/status.ts`.

```ts
import type { AssistantStatus, ComplianceCheck, RiskLevel } from "./types";

export const assistantStatusLabels: Record<AssistantStatus, string> = {
  NotStarted: "Не проверено",
  InProgress: "В работе",
  NeedsReview: "Требует проверки",
  Reviewed: "Проверено",
  Error: "Ошибка",
};

export function getAssistantStatusFromCheck(
  check?: ComplianceCheck | null,
): AssistantStatus {
  if (!check) return "NotStarted";
  if (check.reviewedByHuman) return "Reviewed";

  const issuesCount = check.riskSummary?.issues?.length ?? 0;
  if (issuesCount > 0) return "NeedsReview";

  return "Reviewed";
}

export function getAssistantStatusClass(status: AssistantStatus): string {
  switch (status) {
    case "NotStarted":
      return "border-slate-200 bg-slate-50 text-slate-600";
    case "InProgress":
      return "border-blue-200 bg-blue-50 text-blue-700";
    case "NeedsReview":
      return "border-amber-200 bg-amber-50 text-amber-700";
    case "Reviewed":
      return "border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Error":
      return "border-red-200 bg-red-50 text-red-700";
  }
}

export function getRiskLevelClass(level: RiskLevel): string {
  switch (level) {
    case "Low":
      return "border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Medium":
      return "border-blue-200 bg-blue-50 text-blue-700";
    case "High":
      return "border-amber-200 bg-amber-50 text-amber-700";
    case "Critical":
      return "border-red-200 bg-red-50 text-red-700";
  }
}
```

---

## 12. Format helpers

Create `src/lib/formatters.ts`.

```ts
export function formatMoney(amount?: number | null, currency?: string | null) {
  if (amount == null) return "—";

  return new Intl.NumberFormat("ru-KZ", {
    maximumFractionDigits: 2,
  }).format(amount) + (currency ? ` ${currency}` : "");
}

export function formatDateTime(value?: string | null) {
  if (!value) return "—";

  return new Intl.DateTimeFormat("ru-KZ", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

export function formatDate(value?: string | null) {
  if (!value) return "—";

  return new Intl.DateTimeFormat("ru-KZ", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(new Date(value));
}
```

---

## 13. Dashboard page behavior

Create `/src/app/compliance/page.tsx` as a client component or wrap client logic in `ComplianceDashboard`.

Important behavior:

1. Start with `mockCases`.
2. Show existing dashboard columns.
3. Show AI-added columns:
   - `Статус ассистента`
   - `Risk Score`
   - `Risk Level`
   - `Issues`
   - `Отчет`
4. On `Run Assistant`:
   - set selected row to `InProgress`;
   - call `POST /api/compliance-checks/demo` for demo stability;
   - attach first returned `ComplianceCheck` to that row;
   - set status to `NeedsReview` if issues exist;
   - save returned check ID;
   - allow opening report.

Pseudo-code:

```tsx
"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { createDemoComplianceChecks } from "@/lib/api";
import { mockCases } from "@/lib/mockCases";
import type { DashboardCase } from "@/lib/types";
import { getAssistantStatusFromCheck } from "@/lib/status";

export default function CompliancePage() {
  const router = useRouter();
  const [cases, setCases] = useState<DashboardCase[]>(mockCases);

  async function runAssistant(sourceId: number) {
    setCases((prev) =>
      prev.map((item) =>
        item.sourceId === sourceId
          ? { ...item, assistantStatus: "InProgress" }
          : item,
      ),
    );

    try {
      const checks = await createDemoComplianceChecks();
      const check = checks[0];

      setCases((prev) =>
        prev.map((item) =>
          item.sourceId === sourceId
            ? {
                ...item,
                check,
                checkId: check.id,
                assistantStatus: getAssistantStatusFromCheck(check),
              }
            : item,
        ),
      );

      router.push(`/compliance/${check.id}`);
    } catch {
      setCases((prev) =>
        prev.map((item) =>
          item.sourceId === sourceId
            ? { ...item, assistantStatus: "Error" }
            : item,
        ),
      );
    }
  }

  return (
    <main className="min-h-screen bg-slate-50 p-6">
      {/* render dashboard */}
    </main>
  );
}
```

---

## 14. Dashboard UI layout

Page title:

```text
Currency Control Dashboard
```

Subtitle:

```text
Default compliance queue with AI-assisted document review
```

Small disclaimer:

```text
AI output is preliminary. Final decision is made by a compliance specialist.
```

Top summary cards:

```text
Incoming cases: 3
AI in progress: 0/1
Needs review: 0/1
High risk: 0/1
```

Main table columns:

```text
Case ID
Client
Files
Amount
Received
Base Status
Assistant Status
Risk Score
Risk Level
Issues
Actions
```

Important: make it visually obvious that `Assistant Status`, `Risk Score`, `Risk Level`, and `Issues` are the new AI layer.

Possible column group labels:

```text
Existing dashboard fields
AI Assistant layer
```

---

## 15. Dashboard table row example

Before AI:

```text
VC-2026-0142 | GreenMarket Retail LLP | invoice_0147.pdf, payment_order_0142.xlsx | 10,000 USD | На проверке | Не проверено | — | — | — | Run Assistant
```

During AI:

```text
VC-2026-0142 | GreenMarket Retail LLP | ... | 10,000 USD | На проверке | В работе | — | — | — | Analyzing...
```

After AI:

```text
VC-2026-0142 | GreenMarket Retail LLP | ... | 10,000 USD | На проверке | Требует проверки | 85/100 | High | 4 | Посмотреть отчет
```

After human review:

```text
VC-2026-0142 | GreenMarket Retail LLP | ... | 10,000 USD | На проверке | Проверено | 85/100 | High | 4 | Посмотреть отчет
```

---

## 16. Assistant status badge component

Create `src/components/dashboard/AssistantStatusBadge.tsx`.

```tsx
import type { AssistantStatus } from "@/lib/types";
import {
  assistantStatusLabels,
  getAssistantStatusClass,
} from "@/lib/status";

export function AssistantStatusBadge({ status }: { status: AssistantStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium ${getAssistantStatusClass(status)}`}
    >
      {assistantStatusLabels[status]}
    </span>
  );
}
```

---

## 17. Risk badge component

Create `src/components/dashboard/RiskLevelBadge.tsx`.

```tsx
import type { RiskLevel } from "@/lib/types";
import { getRiskLevelClass } from "@/lib/status";

export function RiskLevelBadge({ level }: { level?: RiskLevel | null }) {
  if (!level) return <span className="text-slate-400">—</span>;

  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium ${getRiskLevelClass(level)}`}
    >
      {level}
    </span>
  );
}
```

---

## 18. File badges

Create `FileListCell.tsx`.

```tsx
import { FileSpreadsheet, FileText } from "lucide-react";
import type { DashboardFile } from "@/lib/types";

export function FileListCell({ files }: { files: DashboardFile[] }) {
  return (
    <div className="flex flex-col gap-1">
      {files.map((file) => (
        <div key={file.name} className="flex items-center gap-2 text-xs text-slate-600">
          {file.type === "xlsx" ? (
            <FileSpreadsheet className="h-3.5 w-3.5" />
          ) : (
            <FileText className="h-3.5 w-3.5" />
          )}
          <span>{file.name}</span>
        </div>
      ))}
    </div>
  );
}
```

---

## 19. Report details page

Route:

```text
/compliance/[id]
```

Use `GET /api/compliance-checks/{id}`.

Page sections:

1. Header
2. AI summary
3. Extracted fields
4. Checklist
5. Risk issues
6. Explanation
7. Draft client message
8. Human review controls

Page title:

```text
AI Compliance Report
```

Subtitle:

```text
Preliminary assistant analysis for currency control document package
```

Disclaimer:

```text
This report is AI-generated and must be reviewed by a compliance specialist before any action is taken.
```

---

## 20. Report page layout

Recommended layout:

```text
[Back to dashboard]

AI Compliance Report
Case VC-2026-0142 · GreenMarket Retail LLP

[Risk Score 85/100] [Risk Level High] [Issues 4] [Human Review: Pending]

Left column:
- Extracted Fields
- Checklist

Right column:
- Risk Issues
- Explanation
- Draft Client Message
- Human Review
```

Use responsive layout:

```text
Desktop: 2 columns
Mobile: 1 column
```

---

## 21. Extracted fields panel

Since API has `invoice` and `paymentOrder`, show them side by side.

Fields to show:

Invoice:

```text
Invoice number
Invoice date
Due date
Seller name
Seller BIN
Seller bank
Seller IBAN
Buyer name
Buyer BIN
Contract number
Contract date
Subtotal
VAT
Total amount
Currency
```

Payment order:

```text
Payment date
Payer name
Payer BIN
Payer bank
Payer IBAN
Receiver name
Receiver BIN
Receiver bank
Receiver IBAN
Amount
Currency
Payment purpose
```

If value is missing, show:

```text
—
```

Missing values should be visually subtle but noticeable.

---

## 22. Checklist panel

Render `checklist` array.

For each item:

```text
✅ if passed
⚠️ if not passed
label
details
```

If checklist is empty:

```text
No checklist items returned by the assistant.
```

---

## 23. Risk summary panel

Render:

```text
Risk Score: riskSummary.score / 100
Risk Level: riskSummary.level
Detected issues: riskSummary.issues.length
```

For each issue:

```text
Title
Severity badge
Message
Evidence
Recommended action
Score impact
```

Example:

```text
Amount mismatch
Severity: High
Evidence: Invoice amount 10,500 USD differs from payment amount 10,000 USD.
Recommended action: Request corrected invoice or additional agreement.
```

---

## 24. Draft client message panel

Use editable textarea.

Initial value:

```ts
check.draftClientMessage ?? ""
```

Buttons:

```text
Copy
Save as final message / Mark as reviewed
```

Do not send actual email. This is only a draft.

---

## 25. Human review panel

User can enter optional comment:

```text
Reviewed by compliance specialist. Client clarification required.
```

Button:

```text
Mark as reviewed
```

On click:

```ts
await reviewComplianceCheck(check.id, {
  approvedByHuman: true,
  humanComment,
  editedClientMessage,
});
```

After success:

- show `Проверено` badge;
- show final client message;
- disable review button;
- show timestamp locally if useful.

Important: `approvedByHuman` here means the assistant report was reviewed, not that the payment was approved. Avoid wording that implies payment approval.

Better UI label:

```text
Mark report as reviewed
```

Russian:

```text
Отметить отчет как проверенный
```

---

## 26. Local state / persistence note

For hackathon demo, the dashboard starts from mock cases. When user runs assistant, the returned backend check can be attached to the row.

Since navigating from dashboard to report page fetches the check by ID from backend, dashboard local state may reset on refresh. That is acceptable for MVP.

If needed, store a small mapping in `localStorage`:

```ts
sourceId -> checkId
sourceId -> assistantStatus
```

But do not overbuild this unless demo requires it.

---

## 27. Error states

If API call fails:

- set `assistantStatus` to `Error`;
- show a small error message near the row;
- allow retry.

Do not crash the page.

Example UI:

```text
Ошибка проверки. Попробовать снова.
```

---

## 28. Loading states

When assistant is running:

- show `В работе` badge;
- disable button;
- action button text: `Analyzing...` or `Проверяем...`;
- optionally show spinner.

Do not navigate until API returns.

---

## 29. Demo endpoint strategy

For the hackathon demo, prefer `POST /api/compliance-checks/demo` because it is stable.

The dashboard can still show PDF/XLSX filenames to simulate real document packages.

Button behavior:

```text
Run Assistant -> demo endpoint -> attach result -> open report
```

Later, when real file parsing is added, replace this with:

```text
POST /api/compliance-checks
```

using structured `ComplianceCaseInput[]`.

---

## 30. Example ComplianceCaseInput for non-demo endpoint

Use when testing `POST /api/compliance-checks` directly.

```ts
const payload = [
  {
    id: 101,
    invoice: {
      inv_number: "INV-2026-0147",
      inv_date: "2026-05-27",
      inv_due_date: "2026-06-03",
      inv_currency: "USD",
      inv_subtotal: 10000,
      inv_vat: 500,
      inv_total: 10500,
      inv_seller_name: "AlmaTech Solutions LLP",
      inv_seller_bin: "240540012345",
      inv_seller_bank: "Halyk Bank Kazakhstan",
      inv_seller_iban: "KZ859650000012345678",
      inv_buyer_name: "GreenMarket Retail LLP",
      inv_buyer_bin: "220140098765",
      inv_contract_no: "GM-AT-05/2026",
      inv_contract_date: "2026-05-20",
    },
    payment_order: {
      pay_date: "2026-05-15",
      pay_payer_name: "GreenMarket Retail LLP",
      pay_payer_bin: "220140098765",
      pay_payer_bank: "Bereke Bank",
      pay_payer_iban: "KZ129140000098765432",
      pay_receiver_name: "AlmaTech Solutions LLP",
      pay_receiver_bin: "240540012345",
      pay_receiver_bank: "Halyk Bank Kazakhstan",
      pay_receiver_iban: "KZ859650000012345678",
      pay_amount: 10000,
      pay_currency: "USD",
      pay_purpose: "Payment",
    },
  },
];
```

---

## 31. UI copy

Dashboard note:

```text
This dashboard simulates the existing compliance queue. The AI assistant adds document checks, risk scoring and a report view on top of the current workflow.
```

Assistant disclaimer:

```text
AI output is preliminary and must be reviewed by a compliance specialist.
```

Report note:

```text
The assistant found potential inconsistencies in the submitted documents. Review the evidence and edit the client message before taking action.
```

Empty state:

```text
Assistant has not checked this case yet.
```

In progress:

```text
Assistant is analyzing invoice and payment order data...
```

Needs review:

```text
Assistant found issues that require human review.
```

Reviewed:

```text
Report reviewed by compliance specialist.
```

---

## 32. Styling guidelines

Look and feel:

```text
banking
internal tool
clean
serious
low-noise
```

Use:

```text
white cards
soft gray borders
slate text
small badges
clear spacing
simple tables
```

Avoid:

```text
bright startup landing page
heavy gradients
game-like UI
large animations
```

---

## 33. Minimal components to build first

Build in this order:

1. `types.ts`
2. `api.ts`
3. `mockCases.ts`
4. `AssistantStatusBadge.tsx`
5. `RiskLevelBadge.tsx`
6. `/compliance/page.tsx`
7. `/compliance/[id]/page.tsx`
8. `DraftClientMessagePanel.tsx`
9. `HumanReviewPanel.tsx`

Do not start with fancy components. Make the end-to-end flow work first.

---

## 34. MVP acceptance criteria

The frontend is ready if:

1. `/compliance` opens a default compliance dashboard.
2. Dashboard shows PDF/XLSX incoming packages.
3. Dashboard has `Статус ассистента` column.
4. Dashboard has AI preview columns: Risk Score, Risk Level, Issues.
5. User can click `Run Assistant`.
6. Row changes to `В работе`.
7. Frontend calls backend demo endpoint.
8. Row updates to `Требует проверки` with risk score.
9. User can click `Посмотреть отчет`.
10. Report page shows full AI output.
11. User can edit the draft client message.
12. User can mark report as reviewed.
13. UI clearly says final decision stays with human.

---

## 35. Demo script

Use this exact flow during the presentation:

```text
1. Open Currency Control Dashboard.
2. Explain that this is the existing compliance queue.
3. Point to incoming PDF/XLSX document packages.
4. Point to the new AI columns: Assistant Status, Risk Score, Risk Level.
5. Click Run Assistant on GreenMarket Retail LLP.
6. Status changes to В работе.
7. After processing, status becomes Требует проверки and risk score appears.
8. Click Посмотреть отчет.
9. Show extracted invoice and payment order fields.
10. Show checklist failures.
11. Show risk issues and evidence.
12. Show draft client clarification message.
13. Edit the message slightly.
14. Click Mark report as reviewed.
15. End with: AI does not decide. It helps the specialist review faster.
```

---

## 36. Final implementation reminder

This is a hackathon MVP.

Prioritize:

```text
clear workflow
stable demo
visible business value
human-in-the-loop
```

Do not prioritize:

```text
authentication
real file upload
complex table engine
full production design
email sending
payment approval logic
```

The strongest version of the frontend is simple:

```text
Dashboard with AI columns -> Report page -> Human review
```

## Let user load the data in pdf and xlsx

## Also keep "dataset" in folder "data" locally
