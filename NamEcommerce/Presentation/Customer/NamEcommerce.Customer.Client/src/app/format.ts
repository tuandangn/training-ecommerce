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

export function orderStatusText(value: number) {
  switch (value) {
    case 0:
      return "Đang xử lý";
    case 1:
      return "Hoàn thành";
    case 2:
      return "Đã hủy";
    default:
      return statusText(value);
  }
}

export function orderRequestStatusText(value: number) {
  switch (value) {
    case 0:
      return "Chờ cửa hàng duyệt";
    case 1:
      return "Đã duyệt - chờ xác nhận";
    case 2:
      return "Đã từ chối";
    case 3:
      return "Đã tạo đơn";
    case 4:
      return "Đã hủy";
    default:
      return statusText(value);
  }
}

export function deliveryNoteStatusText(value: number) {
  switch (value) {
    case 10:
      return "Bản nháp";
    case 20:
      return "Đã xác nhận";
    case 30:
      return "Đang giao";
    case 40:
      return "Đã giao";
    case 50:
      return "Đã hủy";
    default:
      return statusText(value);
  }
}
