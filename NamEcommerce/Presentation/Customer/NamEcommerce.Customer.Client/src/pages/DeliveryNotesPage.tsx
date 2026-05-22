import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { DeliveryNoteSummary } from "../api/types";
import { deliveryNoteStatusText, shortDate } from "../app/format";
import { navigate } from "../app/routes";

export function DeliveryNotesPage() {
  const [notes, setNotes] = useState<DeliveryNoteSummary[]>([]);

  useEffect(() => {
    apiFetch<{ items: DeliveryNoteSummary[] }>("/api/delivery-notes").then((result) => setNotes(result.items));
  }, []);

  return (
    <section>
      <h1 className="page-title">Phiếu giao hàng</h1>
      <p className="page-subtitle">Trạng thái giao nhận</p>
      <table className="table">
        <thead>
          <tr>
            <th>Mã phiếu</th>
            <th>Đơn hàng</th>
            <th>Trạng thái</th>
            <th>Ngày tạo</th>
          </tr>
        </thead>
        <tbody>
          {notes.map((note) => (
            <tr key={note.id} onClick={() => navigate(`/delivery-notes/${note.id}`)}>
              <td>{note.code}</td>
              <td>{note.orderCode ?? "-"}</td>
              <td>
                <span className="badge">{deliveryNoteStatusText(note.status)}</span>
              </td>
              <td>{shortDate(note.createdOn)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
