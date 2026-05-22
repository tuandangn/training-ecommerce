import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { OrderRequestSummary, OrderSummary } from "../api/types";
import { money, orderRequestStatusText, orderStatusText, shortDate } from "../app/format";
import { navigate } from "../app/routes";

export function OrdersPage() {
  const [orders, setOrders] = useState<OrderSummary[]>([]);
  const [orderRequests, setOrderRequests] = useState<OrderRequestSummary[]>([]);

  useEffect(() => {
    void loadData();
  }, []);

  async function loadData() {
    const [ordersResult, requestsResult] = await Promise.allSettled([
      apiFetch<{ items: OrderSummary[] }>("/api/orders"),
      apiFetch<{ items: OrderRequestSummary[] }>("/api/order-requests"),
    ]);

    if (ordersResult.status === "fulfilled") setOrders(ordersResult.value.items);
    if (requestsResult.status === "fulfilled") setOrderRequests(requestsResult.value.items);
  }

  const confirmableRequests = orderRequests.filter((request) => request.canConfirm);

  return (
    <section className="stack">
      <div className="toolbar">
        <div>
          <h1 className="page-title">Đơn hàng</h1>
          <p className="page-subtitle">Yêu cầu đặt hàng và đơn đã tạo</p>
        </div>
        <button className="button primary" onClick={() => navigate("/orders/new")}>
          + Đặt hàng
        </button>
      </div>
      {confirmableRequests.length > 0 && (
        <div className="notice success">
          Có {confirmableRequests.length} yêu cầu đã được cửa hàng duyệt. Vui lòng xem báo giá và xác nhận để tạo đơn.
        </div>
      )}
      <section className="card stack">
        <div className="toolbar">
          <div>
            <h2 className="page-title">Yêu cầu đặt hàng</h2>
            <p className="page-subtitle">Các yêu cầu đang chờ duyệt hoặc chờ bạn xác nhận</p>
          </div>
        </div>
        <table className="table">
          <thead>
            <tr>
              <th>Mã yêu cầu</th>
              <th>Trạng thái</th>
              <th>Tổng tiền</th>
              <th>Ngày tạo</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {orderRequests.length === 0 && (
              <tr>
                <td colSpan={5}>Chưa có yêu cầu đặt hàng.</td>
              </tr>
            )}
            {orderRequests.map((request) => (
              <tr key={request.id}>
                <td>{request.code}</td>
                <td>
                  <span className="badge">{orderRequestStatusText(request.status)}</span>
                </td>
                <td>{request.totalAmount === null || request.totalAmount === undefined ? "Chờ báo giá" : money(request.totalAmount)}</td>
                <td>{shortDate(request.createdOn)}</td>
                <td>
                  <button className="button" type="button" onClick={() => navigate(`/order-requests/${request.id}`)}>
                    Xem
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
      <section className="card stack">
        <div>
          <h2 className="page-title">Đơn đã tạo</h2>
          <p className="page-subtitle">Đơn hàng nội bộ sau khi được xác nhận</p>
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
          {orders.length === 0 && (
            <tr>
              <td colSpan={4}>Chưa có đơn hàng.</td>
            </tr>
          )}
          {orders.map((order) => (
            <tr key={order.id} onClick={() => navigate(`/orders/${order.id}`)}>
              <td>{order.code}</td>
              <td>
                <span className="badge">{orderStatusText(order.status)}</span>
              </td>
              <td>{money(order.totalAmount)}</td>
              <td>{shortDate(order.createdOn)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      </section>
    </section>
  );
}
