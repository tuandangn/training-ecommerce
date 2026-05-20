import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { OtpRequestResult, PublicDeliveryNote } from "../api/types";
import { navigate } from "../app/routes";
import { shortDate, statusText } from "../app/format";

export function PublicDeliveryPage({ token }: { token: string }) {
  const [note, setNote] = useState<PublicDeliveryNote | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    apiFetch<PublicDeliveryNote>(`/api/public/delivery-notes/${encodeURIComponent(token)}`)
      .then(setNote)
      .catch(() => setError("Không tìm thấy phiếu giao hàng."))
      .finally(() => setLoading(false));
  }, [token]);

  async function requestOtp() {
    const result = await apiFetch<OtpRequestResult>("/api/auth/otp/request", {
      method: "POST",
      body: JSON.stringify({ deliveryToken: token }),
    });
    if (result.success && result.challengeId) {
      const query = new URLSearchParams({ challengeId: result.challengeId });
      if (result.mockOtp) query.set("mockOtp", result.mockOtp);
      navigate(`/verify?${query.toString()}`);
    }
  }

  return (
    <main className="public-wrap">
      <section className="public-panel stack">
        <div>
          <h1 className="page-title">Phiếu giao hàng</h1>
          <p className="page-subtitle">VLXD Tuấn Khôi</p>
        </div>
        {loading && <div>Đang tải...</div>}
        {error && <div className="notice">{error}</div>}
        {note && (
          <>
            <div className="grid cols-3">
              <div className="card">
                <div className="metric-label">Mã phiếu</div>
                <div className="metric-value">{note.code}</div>
              </div>
              <div className="card">
                <div className="metric-label">Đơn hàng</div>
                <div className="metric-value">{note.orderCode ?? "-"}</div>
              </div>
              <div className="card">
                <div className="metric-label">Trạng thái</div>
                <div className="metric-value">{statusText(note.status)}</div>
              </div>
            </div>
            <table className="table">
              <thead>
                <tr>
                  <th>Hàng hóa</th>
                  <th>Số lượng</th>
                </tr>
              </thead>
              <tbody>
                {note.items.map((item) => (
                  <tr key={item.id}>
                    <td>{item.productName}</td>
                    <td>{item.quantity}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="toolbar">
              <span className="page-subtitle">Ngày tạo: {shortDate(note.createdOn)}</span>
              <button className="button primary" onClick={requestOtp}>
                Xác thực
              </button>
            </div>
          </>
        )}
      </section>
    </main>
  );
}
