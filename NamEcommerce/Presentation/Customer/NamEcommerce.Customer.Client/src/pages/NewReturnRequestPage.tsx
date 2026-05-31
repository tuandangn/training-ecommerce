import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { apiFetch } from "../api/client";
import type { ReturnableItem, ReturnableItemList } from "../api/types";
import { money, quantity } from "../app/format";
import { navigate } from "../app/routes";

type ReturnPictureDraft = {
  fileName: string;
  mimeType: string;
  base64Data: string;
  previewUrl: string;
};

const MAX_RETURN_PICTURES_PER_ITEM = 3;
const MAX_RETURN_PICTURE_BYTES = 5 * 1024 * 1024;
const ALLOWED_RETURN_PICTURE_TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);

export function NewReturnRequestPage() {
  const [items, setItems] = useState<ReturnableItem[]>([]);
  const [quantities, setQuantities] = useState<Record<string, string>>({});
  const [pictures, setPictures] = useState<Record<string, ReturnPictureDraft[]>>({});
  const [reason, setReason] = useState("");
  const [compensateInNextDelivery, setCompensateInNextDelivery] = useState(false);
  const [message, setMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    apiFetch<ReturnableItemList>("/api/return-requests/returnable-items")
      .then((result) => {
        setItems(result.items);
        setQuantities(Object.fromEntries(result.items.map((item) => [item.productId, "0"])));
      })
      .catch(() => {
        setItems([]);
        setMessage("Không thể tải danh sách hàng đã giao.");
      });
  }, []);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const selectedItems = items
      .map((item) => ({
        productId: item.productId,
        requestedQuantity: parseQuantity(quantities[item.productId]),
        maxQuantity: item.returnableQuantity,
        evidencePictures: pictures[item.productId] ?? [],
      }))
      .filter((item) => item.requestedQuantity > 0);

    if (selectedItems.length === 0) {
      setMessage("Vui lòng nhập số lượng cần trả.");
      return;
    }

    if (selectedItems.some((item) => item.requestedQuantity > item.maxQuantity)) {
      setMessage("Số lượng trả không được lớn hơn số lượng còn có thể trả.");
      return;
    }

    setSubmitting(true);
    setMessage("");

    try {
      const created = await apiFetch<{ id: string }>("/api/return-requests", {
        method: "POST",
        body: JSON.stringify({
          reason,
          compensateInNextDelivery,
          items: selectedItems.map(({ productId, requestedQuantity, evidencePictures }) => ({
            productId,
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
      navigate(`/return-requests/${created.id}`);
    } catch (error) {
      setMessage(getApiErrorMessage(error, "Không thể gửi yêu cầu trả hàng lúc này."));
    } finally {
      setSubmitting(false);
    }
  }

  async function addPictures(productId: string, files: FileList | null) {
    if (!files || files.length === 0) return;

    const existing = pictures[productId] ?? [];
    const selected = Array.from(files);
    if (existing.length + selected.length > MAX_RETURN_PICTURES_PER_ITEM) {
      setMessage(`Mỗi mặt hàng chỉ được gửi tối đa ${MAX_RETURN_PICTURES_PER_ITEM} ảnh.`);
      return;
    }

    const invalidFile = selected.find((file) => !ALLOWED_RETURN_PICTURE_TYPES.has(file.type) || file.size > MAX_RETURN_PICTURE_BYTES);
    if (invalidFile) {
      setMessage("Ảnh hiện trạng chỉ nhận JPG, PNG, WEBP và tối đa 5MB mỗi ảnh.");
      return;
    }

    const drafts = await Promise.all(selected.map(readReturnPictureDraft));
    setPictures((current) => ({
      ...current,
      [productId]: [...(current[productId] ?? []), ...drafts],
    }));
    setMessage("");
  }

  function removePicture(productId: string, index: number) {
    setPictures((current) => ({
      ...current,
      [productId]: (current[productId] ?? []).filter((_, pictureIndex) => pictureIndex !== index),
    }));
  }

  return (
    <section className="stack">
      <div className="toolbar">
        <div>
          <h1 className="page-title">Tạo yêu cầu trả hàng</h1>
          <p className="page-subtitle">Chọn từ các hàng đã giao còn có thể trả</p>
        </div>
        <button className="button" type="button" onClick={() => navigate("/return-requests")}>
          Quay lại
        </button>
      </div>

      {message && <div className="notice">{message}</div>}

      <form className="stack" onSubmit={submit}>
        <section className="card stack">
          <table className="table">
            <thead>
              <tr>
                <th>Hàng hóa</th>
                <th>Đã giao</th>
                <th>Còn trả được</th>
                <th>Đơn giá gần nhất</th>
                <th>Số lượng trả</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 && (
                <tr>
                  <td colSpan={5}>Không có hàng đã giao còn có thể trả.</td>
                </tr>
              )}
              {items.map((item) => (
                <ReturnableProductRow
                  item={item}
                  key={item.productId}
                  pictures={pictures[item.productId] ?? []}
                  value={quantities[item.productId] ?? "0"}
                  onChange={(value) => setQuantities((current) => ({ ...current, [item.productId]: value }))}
                  onPicturesChange={(files) => void addPictures(item.productId, files)}
                  onRemovePicture={(index) => removePicture(item.productId, index)}
                />
              ))}
            </tbody>
          </table>
        </section>

        <div className="field">
          <label>Lý do</label>
          <textarea value={reason} onChange={(event) => setReason(event.target.value)} />
        </div>

        <label className="checkbox-field">
          <input
            type="checkbox"
            checked={compensateInNextDelivery}
            onChange={(event) => setCompensateInNextDelivery(event.target.checked)}
          />
          <span>Bù số lượng trả lại vào lần giao sau</span>
        </label>

        <div className="modal-actions">
          <button className="button" type="button" onClick={() => navigate("/return-requests")}>
            Hủy
          </button>
          <button className="button danger" type="submit" disabled={submitting || items.length === 0}>
            Gửi yêu cầu
          </button>
        </div>
      </form>
    </section>
  );
}

function ReturnableProductRow({
  item,
  value,
  pictures,
  onChange,
  onPicturesChange,
  onRemovePicture,
}: {
  item: ReturnableItem;
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
        <td>{quantity(item.deliveredQuantity)} {item.unit}</td>
        <td>
          {quantity(item.returnableQuantity)} {item.unit}
          {item.reservedReturnQuantity > 0 && (
            <div className="page-subtitle">Đã/đang xử lý {quantity(item.reservedReturnQuantity)}</div>
          )}
        </td>
        <td>{money(item.latestUnitPrice)}</td>
        <td>
          <input
            className="quantity-input"
            inputMode="decimal"
            max={item.returnableQuantity}
            min="0"
            step="0.01"
            type="number"
            value={value}
            onChange={(event) => onChange(event.target.value)}
          />
        </td>
      </tr>
      <tr>
        <td colSpan={5}>
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

function parseQuantity(value: string | undefined) {
  if (!value) return 0;

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function getApiErrorMessage(error: unknown, fallback: string) {
  if (error instanceof Error && error.message && !error.message.startsWith("Request failed")) {
    return error.message;
  }

  return fallback;
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
