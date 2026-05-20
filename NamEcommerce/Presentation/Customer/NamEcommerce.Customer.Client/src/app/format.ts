export function money(value: number) {
  return `${Math.round(value).toLocaleString("vi-VN")}đ`;
}

export function shortDate(value?: string | null) {
  if (!value) return "-";
  return new Intl.DateTimeFormat("vi-VN").format(new Date(value));
}

export function statusText(value: number) {
  return `#${value}`;
}
