import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { DebtSummary } from "../api/types";
import { money, shortDate, statusText } from "../app/format";
import { navigate } from "../app/routes";

export function DebtsPage() {
  const [summary, setSummary] = useState<DebtSummary | null>(null);

  useEffect(() => {
    apiFetch<DebtSummary>("/api/debts").then(setSummary);
  }, []);

  if (!summary) return <div>Đang tải...</div>;

  return (
    <section className="stack">
      <div>
        <h1 className="page-title">Công nợ</h1>
        <p className="page-subtitle">Thanh toán và lịch sử gần đây</p>
      </div>
      <div className="grid cols-3">
        <div className="card">
          <div className="metric-label">Còn nợ</div>
          <div className="metric-value text-danger">{money(summary.totalRemainingAmount)}</div>
        </div>
        <div className="card">
          <div className="metric-label">Đã trả</div>
          <div className="metric-value text-success">{money(summary.totalPaidAmount)}</div>
        </div>
        <div className="card">
          <div className="metric-label">Tiền dư</div>
          <div className="metric-value">{money(summary.depositBalance)}</div>
        </div>
      </div>
      <table className="table">
        <thead>
          <tr>
            <th>Mã nợ</th>
            <th>Phiếu giao</th>
            <th>Trạng thái</th>
            <th>Còn lại</th>
            <th>Hạn</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {summary.debts.map((debt) => (
            <tr key={debt.id}>
              <td>{debt.code}</td>
              <td>{debt.deliveryNoteCode ?? "-"}</td>
              <td>
                <span className="badge">{statusText(debt.status)}</span>
              </td>
              <td>{money(debt.remainingAmount)}</td>
              <td>{shortDate(debt.dueDate)}</td>
              <td>
                <button className="button primary" onClick={() => navigate(`/payments?debtId=${debt.id}&amount=${debt.remainingAmount}`)}>
                  Thanh toán
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
