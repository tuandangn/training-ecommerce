import { FormEvent, useState } from "react";
import { apiFetch } from "../api/client";
import { navigate } from "../app/routes";

type DraftItem = {
  productId: string;
  quantity: number;
};

export function NewOrderRequestPage() {
  const [shippingAddress, setShippingAddress] = useState("");
  const [note, setNote] = useState("");
  const [items, setItems] = useState<DraftItem[]>([{ productId: "", quantity: 1 }]);
  const [message, setMessage] = useState("");

  function updateItem(index: number, patch: Partial<DraftItem>) {
    setItems((current) => current.map((item, itemIndex) => (itemIndex === index ? { ...item, ...patch } : item)));
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    await apiFetch("/api/order-requests", {
      method: "POST",
      body: JSON.stringify({
        shippingAddress,
        note,
        items: items.filter((item) => item.productId),
      }),
    });
    setMessage("Đã gửi yêu cầu đặt hàng.");
  }

  return (
    <section className="card">
      <h1 className="page-title">Đặt hàng</h1>
      <p className="page-subtitle">Chờ duyệt trước khi xử lý</p>
      {message && (
        <div className="toolbar">
          <span className="badge">{message}</span>
          <button className="button" onClick={() => navigate("/orders")}>
            Danh sách đơn
          </button>
        </div>
      )}
      <form onSubmit={submit}>
        <div className="field">
          <label>Địa chỉ giao</label>
          <input value={shippingAddress} onChange={(event) => setShippingAddress(event.target.value)} />
        </div>
        <div className="field">
          <label>Ghi chú</label>
          <textarea value={note} onChange={(event) => setNote(event.target.value)} />
        </div>
        <table className="table">
          <thead>
            <tr>
              <th>Mã hàng</th>
              <th>Số lượng</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item, index) => (
              <tr key={index}>
                <td>
                  <input value={item.productId} onChange={(event) => updateItem(index, { productId: event.target.value })} />
                </td>
                <td>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={item.quantity}
                    onChange={(event) => updateItem(index, { quantity: Number(event.target.value) })}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <div className="toolbar">
          <button className="button" type="button" onClick={() => setItems((current) => [...current, { productId: "", quantity: 1 }])}>
            + Thêm dòng
          </button>
          <button className="button primary" type="submit">
            Gửi yêu cầu
          </button>
        </div>
      </form>
    </section>
  );
}
