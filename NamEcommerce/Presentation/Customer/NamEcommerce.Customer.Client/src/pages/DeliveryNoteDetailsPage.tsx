import { useEffect, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { apiFetch } from "../api/client";
import type { ActionResult, DeliveryNoteDetails, DeliveryNoteItem } from "../api/types";
import { money, shortDate, statusText } from "../app/format";

type ActiveModal = "received" | "return" | null;

const DELIVERY_CONFIRMATION_CONFIRMED = 2;

export function DeliveryNoteDetailsPage({ id }: { id: string }) {
  const [note, setNote] = useState<DeliveryNoteDetails | null>(null);
  const [receiverName, setReceiverName] = useState("");
  const [confirmNote, setConfirmNote] = useState("");
  const [message, setMessage] = useState("");
  const [returnReason, setReturnReason] = useState("");
  const [returnQuantities, setReturnQuantities] = useState<Record<string, string>>({});
  const [returnMessage, setReturnMessage] = useState("");
  const [activeModal, setActiveModal] = useState<ActiveModal>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    apiFetch<DeliveryNoteDetails>(`/api/delivery-notes/${id}`).then(setNote).catch(() => setNote(null));
  }, [id]);

  async function reloadNote() {
    const updated = await apiFetch<DeliveryNoteDetails>(`/api/delivery-notes/${id}`);
    setNote(updated);
  }

  function openReturnModal() {
    if (!note) return;

    setReturnReason("");
    setReturnMessage("");
    setReturnQuantities(Object.fromEntries(note.items.map((item) => [item.id, "0"])));
    setActiveModal("return");
  }

  async function confirmReceived(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setMessage("");

    try {
      const result = await apiFetch<ActionResult>(`/api/delivery-notes/${id}/confirm`, {
        method: "POST",
        body: JSON.stringify({ receiverName, note: confirmNote }),
      });
      setMessage(result.message ?? "Đã ghi nhận khách đã nhận hàng.");
      setActiveModal(null);
      await reloadNote();
    } catch {
      setMessage("Không thể ghi nhận đã nhận hàng lúc này.");
    } finally {
      setSubmitting(false);
    }
  }

  async function requestReturn(event: FormEvent) {
    event.preventDefault();
    if (!note) return;

    const items = note.items
      .map((item) => ({
        deliveryNoteItemId: item.id,
        requestedQuantity: parseQuantity(returnQuantities[item.id]),
        reason: returnReason,
        maxQuantity: item.quantity,
      }))
      .filter((item) => item.requestedQuantity > 0);

    if (items.length === 0) {
      setReturnMessage("Vui lòng nhập số lượng cần trả.");
      return;
    }

    if (items.some((item) => item.requestedQuantity > item.maxQuantity)) {
      setReturnMessage("Số lượng trả không được lớn hơn số lượng đã nhận.");
      return;
    }

    setSubmitting(true);
    setReturnMessage("");

    try {
      await apiFetch("/api/return-requests", {
        method: "POST",
        body: JSON.stringify({
          deliveryNoteId: note.id,
          reason: returnReason,
          items: items.map(({ deliveryNoteItemId, requestedQuantity, reason }) => ({
            deliveryNoteItemId,
            requestedQuantity,
            reason,
          })),
        }),
      });
      setMessage("Đã gửi yêu cầu trả hàng.");
      setActiveModal(null);
    } catch {
      setReturnMessage("Không thể gửi yêu cầu trả hàng lúc này.");
    } finally {
      setSubmitting(false);
    }
  }

  if (!note) return <div>Đang tải...</div>;

  const receivedConfirmed = note.deliveryConfirmationStatus === DELIVERY_CONFIRMATION_CONFIRMED;

  return (
    <section className="stack">
      <div className="toolbar">
        <div>
          <h1 className="page-title">{note.code}</h1>
          <p className="page-subtitle">
            {shortDate(note.createdOn)} · {statusText(note.status)}
          </p>
        </div>
        <div className="actions">
          <button
            className="button success"
            onClick={() => setActiveModal("received")}
            disabled={receivedConfirmed}
          >
            Đã nhận hàng
          </button>
          <button className="button danger" onClick={openReturnModal}>
            Trả hàng
          </button>
        </div>
      </div>

      {message && <div className="badge">{message}</div>}

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

      {activeModal === "received" && (
        <Modal title="Đã nhận hàng" onClose={() => setActiveModal(null)}>
          <form className="stack" onSubmit={confirmReceived}>
            <p className="page-subtitle">Xác nhận bạn đã nhận hàng của phiếu {note.code}.</p>
            <div className="field">
              <label>Tên người nhận</label>
              <input value={receiverName} onChange={(event) => setReceiverName(event.target.value)} />
            </div>
            <div className="field">
              <label>Ghi chú</label>
              <textarea value={confirmNote} onChange={(event) => setConfirmNote(event.target.value)} />
            </div>
            <div className="modal-actions">
              <button className="button" type="button" onClick={() => setActiveModal(null)}>
                Hủy
              </button>
              <button className="button success" type="submit" disabled={submitting}>
                Gửi xác nhận
              </button>
            </div>
          </form>
        </Modal>
      )}

      {activeModal === "return" && (
        <Modal title="Trả hàng" onClose={() => setActiveModal(null)}>
          <form className="stack" onSubmit={requestReturn}>
            {returnMessage && <div className="notice">{returnMessage}</div>}
            <table className="table">
              <thead>
                <tr>
                  <th>Hàng hóa</th>
                  <th>Đã nhận</th>
                  <th>Số lượng trả</th>
                </tr>
              </thead>
              <tbody>
                {note.items.map((item) => (
                  <ReturnItemRow
                    key={item.id}
                    item={item}
                    value={returnQuantities[item.id] ?? "0"}
                    onChange={(value) =>
                      setReturnQuantities((current) => ({
                        ...current,
                        [item.id]: value,
                      }))
                    }
                  />
                ))}
              </tbody>
            </table>
            <div className="field">
              <label>Lý do</label>
              <textarea value={returnReason} onChange={(event) => setReturnReason(event.target.value)} />
            </div>
            <div className="modal-actions">
              <button className="button" type="button" onClick={() => setActiveModal(null)}>
                Hủy
              </button>
              <button className="button danger" type="submit" disabled={submitting}>
                Gửi yêu cầu
              </button>
            </div>
          </form>
        </Modal>
      )}
    </section>
  );
}

function ReturnItemRow({
  item,
  value,
  onChange,
}: {
  item: DeliveryNoteItem;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <tr>
      <td>{item.productName}</td>
      <td>{item.quantity}</td>
      <td>
        <input
          className="quantity-input"
          inputMode="decimal"
          min="0"
          max={item.quantity}
          step="0.01"
          type="number"
          value={value}
          onChange={(event) => onChange(event.target.value)}
        />
      </td>
    </tr>
  );
}

function Modal({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) {
  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal-panel" role="dialog" aria-modal="true" aria-label={title}>
        <div className="toolbar">
          <h2 className="page-title">{title}</h2>
          <button className="button" onClick={onClose} type="button" aria-label="Đóng">
            X
          </button>
        </div>
        {children}
      </section>
    </div>
  );
}

function parseQuantity(value: string | undefined) {
  if (!value) return 0;

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}
