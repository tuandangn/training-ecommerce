import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { OrderSummary } from "../api/types";
import { money, shortDate, statusText } from "../app/format";
import { navigate } from "../app/routes";

export function OrdersPage() {
  const [orders, setOrders] = useState<OrderSummary[]>([]);

  useEffect(() => {
    apiFetch<{ items: OrderSummary[] }>("/api/orders").then((result) => setOrders(result.items));
  }, []);

  return (
    <section>
      <div className="toolbar">
        <div>
          <h1 className="page-title">Đơn hàng</h1>
          <p className="page-subtitle">Danh sách đã đặt</p>
        </div>
        <button className="button primary" onClick={() => navigate("/orders/new")}>
          + Đặt hàng
        </button>
      </div>
      <table className="table">
        <thead>
          <tr>
            <th>Mã đơn</th>
            <th>Trạng thái</th>
            <th>Tổng tiền</th>
            <th>Ngày tạo</th>
          </tr>
        </thead>
        <tbody>
          {orders.map((order) => (
            <tr key={order.id} onClick={() => navigate(`/orders/${order.id}`)}>
              <td>{order.code}</td>
              <td>
                <span className="badge">{statusText(order.status)}</span>
              </td>
              <td>{money(order.totalAmount)}</td>
              <td>{shortDate(order.createdOn)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
