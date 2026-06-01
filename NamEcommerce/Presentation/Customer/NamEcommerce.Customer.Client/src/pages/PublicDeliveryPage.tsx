import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { ContactInfo, OtpRequestResult, PublicDeliveryNote } from "../api/types";
import { navigate } from "../app/routes";
import { deliveryNoteStatusText, shortDate } from "../app/format";
import { useAuth } from "../auth/useAuth";

export function PublicDeliveryPage({ token }: { token: string }) {
  const [note, setNote] = useState<PublicDeliveryNote | null>(null);
  const [error, setError] = useState("");
  const [contact, setContact] = useState<ContactInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const { refresh } = useAuth();

  useEffect(() => {
    setLoading(true);
    apiFetch<PublicDeliveryNote>(`/api/public/delivery-notes/${encodeURIComponent(token)}`)
      .then(setNote)
      .catch(() => setError("Không tìm thấy phiếu giao hàng."))
      .finally(() => setLoading(false));
  }, [token]);

  useEffect(() => {
    apiFetch<ContactInfo>("/api/contact").then(setContact).catch(() => setContact(null));
  }, []);

  async function requestOtp() {
    if (!note) return;

    const result = await apiFetch<OtpRequestResult>("/api/auth/otp/request", {
      method: "POST",
      body: JSON.stringify({ deliveryToken: token }),
    });

    if (result.success && result.requiresOtp === false) {
      await refresh();
      navigate(`/delivery-notes/${note.id}`);
      return;
    }

    if (result.success && result.challengeId) {
      const query = new URLSearchParams({
        challengeId: result.challengeId,
        returnUrl: `/delivery-notes/${note.id}`,
      });
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
                <div className="metric-value">{deliveryNoteStatusText(note.status)}</div>
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
        {contact && <CompactContact contact={contact} />}
      </section>
    </main>
  );
}

function CompactContact({ contact }: { contact: ContactInfo }) {
  return (
    <section className="card contact-compact">
      <div>
        <div className="metric-label">Liên hệ cửa hàng</div>
        <strong>{contact.store.storeName}</strong>
        <div className="muted-text">{contact.store.phoneNumber || contact.store.email || contact.store.address || "Thông tin sẽ được cập nhật."}</div>
      </div>
      {contact.store.mapQuery && (
        <a className="button" href={mapSearchUrl(contact.store.mapQuery)} target="_blank" rel="noreferrer">
          Bản đồ
        </a>
      )}
    </section>
  );
}

function mapSearchUrl(query: string) {
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(query)}`;
}
