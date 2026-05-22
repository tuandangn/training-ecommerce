import { useMemo, useState } from "react";
import { apiFetch } from "../api/client";
import type { PaymentIntent } from "../api/types";
import { money, statusText } from "../app/format";

export function MockPaymentPage() {
  const query = useMemo(() => new URLSearchParams(window.location.search), []);
  const [amount, setAmount] = useState(Number(query.get("amount") ?? "0"));
  const [intent, setIntent] = useState<PaymentIntent | null>(null);

  async function createIntent() {
    const result = await apiFetch<PaymentIntent>("/api/payment-intents", {
      method: "POST",
      body: JSON.stringify({ customerDebtId: query.get("debtId"), amount }),
    });
    setIntent(result);
  }

  async function complete(success: boolean) {
    if (!intent) return;
    const result = await apiFetch<PaymentIntent>(`/api/payment-intents/${intent.id}/mock-complete`, {
      method: "POST",
      body: JSON.stringify({ success }),
    });
    setIntent(result);
  }

  return (
    <section className="card stack">
      <h1 className="page-title">Thanh toán</h1>
      <p className="page-subtitle">Mock provider</p>
      <div className="field">
        <label>Số tiền</label>
        <input type="number" min="0" value={amount} onChange={(event) => setAmount(Number(event.target.value))} />
      </div>
      {!intent && (
        <button className="button primary" onClick={createIntent}>
          Tạo giao dịch
        </button>
      )}
      {intent && (
        <div className="stack">
          <div className="grid cols-3">
            <div>
              <div className="metric-label">Mã giao dịch</div>
              <div>{intent.providerIntentId ?? intent.id}</div>
            </div>
            <div>
              <div className="metric-label">Số tiền</div>
              <div>{money(intent.amount)}</div>
            </div>
            <div>
              <div className="metric-label">Trạng thái</div>
              <div>{statusText(intent.status)}</div>
            </div>
          </div>
          <div className="toolbar">
            <button className="button success" onClick={() => complete(true)}>
              Thành công
            </button>
            <button className="button danger" onClick={() => complete(false)}>
              Thất bại
            </button>
          </div>
          {intent.status === 2 && <div className="badge">Chờ đối soát</div>}
        </div>
      )}
    </section>
  );
}
