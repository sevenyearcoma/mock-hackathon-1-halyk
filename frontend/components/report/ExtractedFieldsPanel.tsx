import type { ExtractedDocumentFields } from "@/lib/types";
import { formatDate, formatMoney } from "@/lib/formatters";

function Field({ label, value }: { label: string; value?: string | number | null }) {
  const display =
    value == null || value === "" ? (
      <span className="text-slate-300">—</span>
    ) : (
      // break-all ensures long IBANs / BINs wrap instead of overflowing
      <span className="break-all text-slate-800">{String(value)}</span>
    );

  return (
    <div className="flex min-w-0 flex-col gap-0.5">
      <span className="text-xs text-slate-400">{label}</span>
      <span className="text-sm">{display}</span>
    </div>
  );
}

function DocPanel({ title, fields }: { title: string; fields: ExtractedDocumentFields }) {
  const isInvoice = fields.documentType === "Invoice";

  return (
    <div className="min-w-0 rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-3 text-sm font-semibold text-slate-700">{title}</h3>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        {isInvoice ? (
          <>
            <Field label="Номер счёта" value={fields.invoiceNumber} />
            <Field label="Дата счёта" value={formatDate(fields.documentDate)} />
            <Field label="Срок оплаты" value={formatDate(fields.dueDate)} />
            <Field label="Номер договора" value={fields.contractNumber} />
            <Field label="Дата договора" value={formatDate(fields.contractDate)} />
            <Field label="Продавец" value={fields.sellerName} />
            <Field label="БИН продавца" value={fields.sellerBin} />
            <Field label="Банк продавца" value={fields.sellerBank} />
            <Field label="IBAN продавца" value={fields.sellerIban} />
            <Field label="Покупатель" value={fields.buyerName} />
            <Field label="БИН покупателя" value={fields.buyerBin} />
            <Field label="Сумма без НДС" value={formatMoney(fields.subtotal, fields.currency)} />
            <Field label="НДС" value={formatMoney(fields.vat, fields.currency)} />
            <Field label="Итого" value={formatMoney(fields.amount, fields.currency)} />
            <Field label="Валюта" value={fields.currency} />
          </>
        ) : (
          <>
            <Field label="Дата платежа" value={formatDate(fields.documentDate)} />
            <Field label="Плательщик" value={fields.payerName} />
            <Field label="БИН плательщика" value={fields.payerBin} />
            <Field label="Банк плательщика" value={fields.payerBank} />
            <Field label="IBAN плательщика" value={fields.payerIban} />
            <Field label="Получатель" value={fields.receiverName} />
            <Field label="БИН получателя" value={fields.receiverBin} />
            <Field label="Банк получателя" value={fields.receiverBank} />
            <Field label="IBAN получателя" value={fields.receiverIban} />
            <Field label="Сумма" value={formatMoney(fields.amount, fields.currency)} />
            <Field label="Валюта" value={fields.currency} />
            <div className="col-span-2">
              <Field label="Назначение платежа" value={fields.paymentPurpose} />
            </div>
          </>
        )}
      </div>
    </div>
  );
}

export function ExtractedFieldsPanel({
  invoice,
  paymentOrder,
}: {
  invoice: ExtractedDocumentFields;
  paymentOrder: ExtractedDocumentFields;
}) {
  return (
    <div>
      <h2 className="mb-3 text-base font-semibold text-slate-800">Извлечённые данные</h2>
      <div className="flex flex-col gap-3">
        <DocPanel title="Счёт-фактура" fields={invoice} />
        <DocPanel title="Платёжное поручение" fields={paymentOrder} />
      </div>
    </div>
  );
}
