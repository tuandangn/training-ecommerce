import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { LedgerSummary, LedgerStatementItem } from "../api/types";
import { money, shortDate } from "../app/format";

const ENTRY_TYPE_LABEL: Record<number, string> = {
  10: "Giao hàng",
  20: "Đầu kỳ",
  30: "Thu tiền",
  40: "Trả hàng",
  50: "Hoàn tiền",
  60: "Điều chỉnh",
};

export function DebtsPage() {
  const [ledger, setLedger] = useState<LedgerSummary | null>(null);

  useEffect(() => {
    apiFetch<LedgerSummary>("/api/debts/ledger").then(setLedger);
  }, []);

  if (!ledger) return <div>Đang tải...</div>;

  const isDebt = ledger.balance > 0;
  const isCredit = ledger.balance < 0;

  return (
    <section className="stack">
      <div>
        <h1 className="page-title">Công nợ</h1>
        <p className="page-subtitle">Số dư và lịch sử giao dịch</p>
      </div>

      <div className="card">
        <div className="metric-label">Số dư công nợ</div>
        {isDebt && (
          <>
            <div className="metric-value text-danger">{money(ledger.balance)}</div>
            <div className="text-sm text-muted">Bạn còn nợ cửa hàng</div>
          </>
        )}
        {isCredit && (
          <>
            <div className="metric-value text-success">{money(Math.abs(ledger.balance))}</div>
            <div className="text-sm text-muted">Cửa hàng đang giữ tiền thừa của bạn</div>
          </>
        )}
        {!isDebt && !isCredit && (
          <>
            <div className="metric-value">0đ</div>
            <div className="text-sm text-muted">Đã thanh toán đủ</div>
          </>
        )}
        {ledger.lastEntryOnUtc && (
          <div className="text-sm text-muted mt-2">
            Giao dịch cuối: {shortDate(ledger.lastEntryOnUtc)}
          </div>
        )}
      </div>

      {ledger.recentEntries.length > 0 && (
        <div>
          <h2 className="section-title">Sao kê gần đây</h2>
          <table className="table">
            <thead>
              <tr>
                <th>Ngày</th>
                <th>Loại</th>
                <th>Chứng từ</th>
                <th className="text-right">Số dư</th>
              </tr>
            </thead>
            <tbody>
              {ledger.recentEntries.map((entry) => (
                <StatementRow key={entry.entryId} entry={entry} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

function StatementRow({ entry }: { entry: LedgerStatementItem }) {
  const label = ENTRY_TYPE_LABEL[entry.entryType] ?? `#${entry.entryType}`;
  const isCharge = entry.amount > 0;

  return (
    <tr>
      <td className="text-sm">{shortDate(entry.occurredAtUtc)}</td>
      <td>
        <span className="badge">{label}</span>
      </td>
      <td>
        <span className="text-sm">{entry.referenceCode ?? "—"}</span>
        {entry.note && <div className="text-xs text-muted">{entry.note}</div>}
      </td>
      <td className={`text-right font-bold ${isCharge ? "text-danger" : "text-success"}`}>
        {isCharge ? "+" : "-"}{money(Math.abs(entry.amount))}
        <div className="text-xs text-muted font-normal">
          Dư: {money(entry.runningBalance)}
        </div>
      </td>
    </tr>
  );
}
