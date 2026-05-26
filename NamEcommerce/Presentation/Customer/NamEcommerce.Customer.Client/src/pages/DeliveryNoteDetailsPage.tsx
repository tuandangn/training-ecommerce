import { useEffect, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { apiFetch } from "../api/client";
import type { ActionResult, DeliveryNoteDetails, DeliveryNoteItem } from "../api/types";
import { deliveryNoteStatusText, money, shortDate } from "../app/format";

type ActiveModal = "received" | "return" | null;
type ReturnPictureDraft = {
  fileName: string;
  mimeType: string;
  base64Data: string;
  previewUrl: string;
};

const DELIVERY_CONFIRMATION_CONFIRMED = 2;
const MAX_RETURN_PICTURES_PER_ITEM = 3;
const MAX_RETURN_PICTURE_BYTES = 5 * 1024 * 1024;
const ALLOWED_RETURN_PICTURE_TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);

export function DeliveryNoteDetailsPage({ id }: { id: string }) {
  const [note, setNote] = useState<DeliveryNoteDetails | null>(null);
  const [message, setMessage] = useState("");
  const [returnReason, setReturnReason] = useState("");
  const [returnQuantities, setReturnQuantities] = useState<Record<string, string>>({});
  const [returnPictures, setReturnPictures] = useState<Record<string, ReturnPictureDraft[]>>({});
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
    if (!receivedConfirmed) {
      setMessage("Vui lòng xác nhận đã nhận hàng trước khi gửi yêu cầu trả hàng.");
      return;
    }

    setReturnReason("");
    setReturnMessage("");
    setReturnQuantities(Object.fromEntries(note.items.map((item) => [item.id, "0"])));
    setReturnPictures({});
    setActiveModal("return");
  }

  async function confirmReceived(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setMessage("");

    try {
      const result = await apiFetch<ActionResult>(`/api/delivery-notes/${id}/confirm`, {
        method: "POST",
        body: JSON.stringify({}),
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
        evidencePictures: returnPictures[item.id] ?? [],
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
          items: items.map(({ deliveryNoteItemId, requestedQuantity, reason, evidencePictures }) => ({
            deliveryNoteItemId,
            requestedQuantity,
            reason,
            evidencePictures: evidencePictures.map((picture) => ({
              fileName: picture.fileName,
              mimeType: picture.mimeType,
              base64Data: picture.base64Data,
            })),
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

  async function addReturnPictures(itemId: string, files: FileList | null) {
    if (!files || files.length === 0) return;

    const existing = returnPictures[itemId] ?? [];
    const selected = Array.from(files);
    if (existing.length + selected.length > MAX_RETURN_PICTURES_PER_ITEM) {
      setReturnMessage(`Mỗi dòng hàng chỉ được gửi tối đa ${MAX_RETURN_PICTURES_PER_ITEM} ảnh.`);
      return;
    }

    const invalidFile = selected.find((file) => !ALLOWED_RETURN_PICTURE_TYPES.has(file.type) || file.size > MAX_RETURN_PICTURE_BYTES);
    if (invalidFile) {
      setReturnMessage("Ảnh hiện trạng chỉ nhận JPG, PNG, WEBP và tối đa 5MB mỗi ảnh.");
      return;
    }

    const drafts = await Promise.all(selected.map(readReturnPictureDraft));
    setReturnPictures((current) => ({
      ...current,
      [itemId]: [...(current[itemId] ?? []), ...drafts],
    }));
    setReturnMessage("");
  }

  function removeReturnPicture(itemId: string, index: number) {
    setReturnPictures((current) => ({
      ...current,
      [itemId]: (current[itemId] ?? []).filter((_, pictureIndex) => pictureIndex !== index),
    }));
  }

  if (!note) return <div>Đang tải...</div>;

  const receivedConfirmed = note.deliveryConfirmationStatus === DELIVERY_CONFIRMATION_CONFIRMED;

  return (
    <section className="stack">
      <div className="toolbar">
        <div>
          <h1 className="page-title">{note.code}</h1>
          <p className="page-subtitle">
            {shortDate(note.createdOn)} · {deliveryNoteStatusText(note.status)}
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
          <button className="button danger" onClick={openReturnModal} disabled={!receivedConfirmed}>
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
            <p className="page-subtitle">Bạn xác nhận đã nhận đủ hàng của phiếu {note.code}?</p>
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
                    pictures={returnPictures[item.id] ?? []}
                    onChange={(value) =>
                      setReturnQuantities((current) => ({
                        ...current,
                        [item.id]: value,
                      }))
                    }
                    onPicturesChange={(files) => void addReturnPictures(item.id, files)}
                    onRemovePicture={(index) => removeReturnPicture(item.id, index)}
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
  pictures,
  onChange,
  onPicturesChange,
  onRemovePicture,
}: {
  item: DeliveryNoteItem;
  value: string;
  pictures: ReturnPictureDraft[];
  onChange: (value: string) => void;
  onPicturesChange: (files: FileList | null) => void;
  onRemovePicture: (index: number) => void;
}) {
  return (
    <>
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
      <tr>
        <td colSpan={3}>
          <div className="return-evidence">
            <label className="button">
              Chụp/đính kèm ảnh
              <input
                accept="image/jpeg,image/png,image/webp"
                capture="environment"
                multiple
                type="file"
                onChange={(event) => {
                  onPicturesChange(event.target.files);
                  event.currentTarget.value = "";
                }}
              />
            </label>
            <div className="return-evidence-list">
              {pictures.map((picture, index) => (
                <div className="return-evidence-thumb" key={`${picture.fileName}-${index}`}>
                  <img src={picture.previewUrl} alt={picture.fileName} />
                  <button className="button" type="button" onClick={() => onRemovePicture(index)}>
                    Xóa
                  </button>
                </div>
              ))}
            </div>
          </div>
        </td>
      </tr>
    </>
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

function readReturnPictureDraft(file: File) {
  return new Promise<ReturnPictureDraft>((resolve, reject) => {
    const reader = new FileReader();
    reader.onerror = () => reject(reader.error);
    reader.onload = () => {
      const previewUrl = String(reader.result ?? "");
      const commaIndex = previewUrl.indexOf(",");
      resolve({
        fileName: file.name,
        mimeType: file.type,
        base64Data: commaIndex >= 0 ? previewUrl.slice(commaIndex + 1) : previewUrl,
        previewUrl,
      });
    };
    reader.readAsDataURL(file);
  });
}
