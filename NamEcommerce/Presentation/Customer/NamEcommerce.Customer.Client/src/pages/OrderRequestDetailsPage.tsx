import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { ConversionResult, OrderRequestDetails } from "../api/types";
import { money, orderRequestStatusText, shortDate } from "../app/format";
import { navigate } from "../app/routes";

export function OrderRequestDetailsPage({ id }: { id: string }) {
  const [request, setRequest] = useState<OrderRequestDetails | null>(null);
  const [message, setMessage] = useState("");
  const [createdOrderId, setCreatedOrderId] = useState<string | null>(null);
  const [confirming, setConfirming] = useState(false);

  useEffect(() => {
    apiFetch<OrderRequestDetails>(`/api/order-requests/${id}`).then(setRequest).catch(() => setRequest(null));
  }, [id]);

  async function confirmRequest() {
    if (!request || !window.confirm("Xác nhận báo giá và tạo đơn hàng?")) return;

    setConfirming(true);
    setMessage("");
    setCreatedOrderId(null);
    try {
      const result = await apiFetch<ConversionResult>(`/api/order-requests/${request.id}/confirm`, { method: "POST" });
      setMessage(result.message ?? (result.success ? "Đã xác nhận yêu cầu." : "Không thể xác nhận yêu cầu."));
      setCreatedOrderId(result.createdId ?? null);
      if (result.success) {
        const refreshed = await apiFetch<OrderRequestDetails>(`/api/order-requests/${id}`);
        setRequest(refreshed);
      }
    } catch {
      setMessage("Không thể xác nhận yêu cầu.");
    } finally {
      setConfirming(false);
    }
  }

  if (!request) return <div>Đang tải...</div>;

  return (
    <section className="stack">
      <div className="toolbar">
        <div>
          <h1 className="page-title">{request.code}</h1>
          <p className="page-subtitle">
            {shortDate(request.createdOn)} · {orderRequestStatusText(request.status)}
          </p>
        </div>
        <button className="button" onClick={() => navigate("/orders")}>
          Danh sách đơn
        </button>
      </div>
      {message && <div className={createdOrderId ? "notice success" : "notice"}>{message}</div>}
      <div className="grid cols-3">
        <div className="card">
          <div className="metric-label">Tổng báo giá</div>
          <div className="metric-value">{request.totalAmount === null || request.totalAmount === undefined ? "Chờ duyệt" : money(request.totalAmount)}</div>
        </div>
        <div className="card">
          <div className="metric-label">Ngày giao mong muốn</div>
          <div className="metric-value">{shortDate(request.expectedShippingDate)}</div>
        </div>
        <div className="card">
          <div className="metric-label">Trạng thái</div>
          <div className="metric-value">{orderRequestStatusText(request.status)}</div>
        </div>
      </div>
      <section className="card stack">
        <div className="toolbar">
          <div>
            <h2 className="page-title">Hàng hóa</h2>
            <p className="page-subtitle">Giá chỉ hiển thị sau khi cửa hàng duyệt</p>
          </div>
          {request.canConfirm && (
            <button className="button success" disabled={confirming} onClick={confirmRequest}>
              {confirming ? "Đang xác nhận..." : "Xác nhận tạo đơn"}
            </button>
          )}
          {createdOrderId && (
            <button className="button primary" onClick={() => navigate(`/orders/${createdOrderId}`)}>
              Xem đơn hàng
            </button>
          )}
        </div>
        {request.adminNote && <div className="notice success">{request.adminNote}</div>}
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
            {request.items.map((item) => (
              <tr key={item.id}>
                <td>{item.productName}</td>
                <td>{item.quantity}</td>
                <td>{item.unitPrice === null || item.unitPrice === undefined ? "Chờ báo giá" : money(item.unitPrice)}</td>
                <td>{item.subTotal === null || item.subTotal === undefined ? "Chờ báo giá" : money(item.subTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
      <section className="card stack">
        <h2 className="page-title">Thông tin giao hàng</h2>
        <div>
          <div className="metric-label">Địa chỉ giao</div>
          <div>{request.shippingAddress || "-"}</div>
        </div>
        <div>
          <div className="metric-label">Ghi chú</div>
          <div>{request.note || "-"}</div>
        </div>
      </section>
    </section>
  );
}
