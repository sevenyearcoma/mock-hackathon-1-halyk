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

export type DashboardFile = {
  name: string;
  type: "pdf" | "xlsx" | "docx" | "unknown";
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
  error?: string;
};
