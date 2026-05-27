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
