import { FormEvent, useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { ActionResult, DeliveryNoteDetails } from "../api/types";
import { money, shortDate, statusText } from "../app/format";

export function DeliveryNoteDetailsPage({ id }: { id: string }) {
  const [note, setNote] = useState<DeliveryNoteDetails | null>(null);
  const [receiverName, setReceiverName] = useState("");
  const [confirmNote, setConfirmNote] = useState("");
  const [message, setMessage] = useState("");
  const [returnReason, setReturnReason] = useState("");

  useEffect(() => {
    apiFetch<DeliveryNoteDetails>(`/api/delivery-notes/${id}`).then(setNote).catch(() => setNote(null));
  }, [id]);

  async function confirm(event: FormEvent) {
    event.preventDefault();
    const result = await apiFetch<ActionResult>(`/api/delivery-notes/${id}/confirm`, {
      method: "POST",
      body: JSON.stringify({ receiverName, note: confirmNote }),
    });
    setMessage(result.message ?? "");
  }

  async function requestReturn() {
    if (!note) return;
    await apiFetch("/api/return-requests", {
      method: "POST",
      body: JSON.stringify({
        deliveryNoteId: note.id,
        reason: returnReason,
        items: note.items.map((item) => ({
          deliveryNoteItemId: item.id,
          requestedQuantity: item.quantity,
          reason: returnReason,
        })),
      }),
    });
    setReturnReason("Đã gửi yêu cầu trả hàng.");
  }

  if (!note) return <div>Đang tải...</div>;

  return (
    <section className="stack">
      <div>
        <h1 className="page-title">{note.code}</h1>
        <p className="page-subtitle">
          {shortDate(note.createdOn)} · {statusText(note.status)}
        </p>
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
          {note.items.map((item) => (
            <tr key={item.id}>
              <td>{item.productName}</td>
              <td>{item.quantity}</td>
              <td>{money(item.unitPrice)}</td>
              <td>{money(item.subTotal)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <form className="card" onSubmit={confirm}>
        <h2 className="page-title">Xác nhận giao hàng</h2>
        {message && <div className="badge">{message}</div>}
        <div className="field">
          <label>Người nhận</label>
          <input value={receiverName} onChange={(event) => setReceiverName(event.target.value)} />
        </div>
        <div className="field">
          <label>Ghi chú</label>
          <textarea value={confirmNote} onChange={(event) => setConfirmNote(event.target.value)} />
        </div>
        <button className="button success" type="submit">
          Xác nhận
        </button>
      </form>
      <div className="card">
        <h2 className="page-title">Trả hàng</h2>
        <div className="field">
          <label>Lý do</label>
          <textarea value={returnReason} onChange={(event) => setReturnReason(event.target.value)} />
        </div>
        <button className="button danger" onClick={requestReturn}>
          Gửi yêu cầu
        </button>
      </div>
    </section>
  );
}
