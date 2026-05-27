export function formatMoney(amount?: number | null, currency?: string | null) {
  if (amount == null) return "—";
  return (
    new Intl.NumberFormat("ru-KZ", { maximumFractionDigits: 2 }).format(amount) +
    (currency ? ` ${currency}` : "")
  );
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
