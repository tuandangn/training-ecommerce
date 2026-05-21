import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { Dashboard } from "../api/types";
import { money, orderStatusText, shortDate } from "../app/format";
import { navigate } from "../app/routes";

export function DashboardPage() {
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);

  useEffect(() => {
    apiFetch<Dashboard>("/api/me/dashboard").then(setDashboard).catch(() => setDashboard(null));
  }, []);

  if (!dashboard) return <div>Đang tải...</div>;

  return (
    <section className="stack">
      <div>
        <h1 className="page-title">Tổng quan</h1>
        <p className="page-subtitle">VLXD Tuấn Khôi</p>
      </div>
      <div className="grid cols-3">
        <div className="card">
          <div className="metric-label">Công nợ còn lại</div>
          <div className="metric-value text-danger">{money(dashboard.debtSummary.totalRemainingAmount)}</div>
        </div>
        <div className="card">
          <div className="metric-label">Đã thanh toán</div>
          <div className="metric-value text-success">{money(dashboard.debtSummary.totalPaidAmount)}</div>
        </div>
        <div className="card">
          <div className="metric-label">Đơn gần đây</div>
          <div className="metric-value">{dashboard.recentOrders.length}</div>
        </div>
      </div>
      <div className="toolbar">
        <h2 className="page-title">Đơn hàng</h2>
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
          {dashboard.recentOrders.map((order) => (
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
  );
}
