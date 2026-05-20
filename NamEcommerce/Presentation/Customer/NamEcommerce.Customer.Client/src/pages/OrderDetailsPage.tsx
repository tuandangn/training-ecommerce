import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { OrderDetails } from "../api/types";
import { money, shortDate, statusText } from "../app/format";

export function OrderDetailsPage({ id }: { id: string }) {
  const [order, setOrder] = useState<OrderDetails | null>(null);

  useEffect(() => {
    apiFetch<OrderDetails>(`/api/orders/${id}`).then(setOrder).catch(() => setOrder(null));
  }, [id]);

  if (!order) return <div>Đang tải...</div>;

  return (
    <section className="stack">
      <div>
        <h1 className="page-title">{order.code}</h1>
        <p className="page-subtitle">
          {shortDate(order.createdOn)} · {statusText(order.status)}
        </p>
      </div>
      <div className="grid cols-3">
        <div className="card">
          <div className="metric-label">Tổng tiền</div>
          <div className="metric-value">{money(order.totalAmount)}</div>
        </div>
        <div className="card">
          <div className="metric-label">Ngày giao dự kiến</div>
          <div className="metric-value">{shortDate(order.expectedShippingDate)}</div>
        </div>
        <div className="card">
          <div className="metric-label">Trạng thái</div>
          <div className="metric-value">{statusText(order.status)}</div>
        </div>
      </div>
      <table className="table">
        <thead>
          <tr>
            <th>Hàng hóa</th>
            <th>Số lượng</th>
            <th>Đơn giá</th>
            <th>Thành tiền</th>
          </tr>
        </thead>
        <tbody>
          {order.items.map((item) => (
            <tr key={item.id}>
              <td>{item.productName}</td>
              <td>{item.quantity}</td>
              <td>{money(item.unitPrice)}</td>
              <td>{money(item.subTotal)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
