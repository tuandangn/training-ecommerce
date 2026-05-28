import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { ActionResult, ReturnRequestDetails } from "../api/types";
import { quantity, returnRequestStatusText, shortDate } from "../app/format";
import { navigate } from "../app/routes";

export function ReturnRequestDetailsPage({ id }: { id: string }) {
  const [request, setRequest] = useState<ReturnRequestDetails | null>(null);
  const [message, setMessage] = useState("");
  const [cancelling, setCancelling] = useState(false);

  useEffect(() => {
    void reloadRequest().catch(() => setRequest(null));
  }, [id]);

  async function reloadRequest() {
    const updated = await apiFetch<ReturnRequestDetails>(`/api/return-requests/${id}`);
    setRequest(updated);
  }

  async function cancelRequest() {
    if (!request || !window.confirm("Hủy yêu cầu trả hàng này?")) return;

    setCancelling(true);
    setMessage("");
    try {
      const result = await apiFetch<ActionResult>(`/api/return-requests/${request.id}/cancel`, { method: "POST" });
      setMessage(result.message ?? "Đã hủy yêu cầu trả hàng.");
      await reloadRequest();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Không thể hủy yêu cầu trả hàng.");
    } finally {
      setCancelling(false);
    }
  }

  if (!request) return <div>Đang tải...</div>;

  return (
    <section className="stack">
      <div className="toolbar">
        <div>
          <h1 className="page-title">Yêu cầu trả hàng</h1>
          <p className="page-subtitle">
            {request.deliveryNoteCode || "Phiếu giao"} · {shortDate(request.createdOn)}
          </p>
        </div>
        <div className="actions">
          <button className="button" type="button" onClick={() => navigate("/return-requests")}>
            Danh sách trả hàng
          </button>
          <button className="button" type="button" onClick={() => navigate(`/delivery-notes/${request.deliveryNoteId}`)}>
            Xem phiếu giao
          </button>
          {request.status === 0 && (
            <button className="button danger" type="button" disabled={cancelling} onClick={cancelRequest}>
              {cancelling ? "Đang hủy..." : "Hủy yêu cầu"}
            </button>
          )}
        </div>
      </div>

      {message && <div className={request.status === 4 ? "notice success" : "notice"}>{message}</div>}

      <div className="grid cols-3">
        <div className="card">
          <div className="metric-label">Trạng thái</div>
          <div className="metric-value">{returnRequestStatusText(request.status)}</div>
        </div>
        <div className="card">
          <div className="metric-label">Số dòng hàng</div>
          <div className="metric-value">{request.itemCount}</div>
        </div>
        <div className="card">
          <div className="metric-label">Tổng số lượng</div>
          <div className="metric-value">{quantity(request.totalRequestedQuantity)}</div>
        </div>
      </div>

      {request.adminNote && <div className="notice success">{request.adminNote}</div>}
      {request.convertedCustomerReturnId && (
        <div className="notice success">Cửa hàng đã tạo phiếu trả hàng nội bộ từ yêu cầu này.</div>
      )}

      <section className="card stack">
        <div>
          <h2 className="page-title">Hàng cần trả</h2>
          <p className="page-subtitle">Ảnh đính kèm giúp cửa hàng kiểm tra tình trạng hàng</p>
        </div>
        <table className="table">
          <thead>
            <tr>
              <th>Hàng hóa</th>
              <th>Số lượng</th>
              <th>Lý do</th>
              <th>Ảnh</th>
            </tr>
          </thead>
          <tbody>
            {request.items.map((item) => (
              <tr key={item.id}>
                <td>{item.productName}</td>
                <td>{quantity(item.requestedQuantity)}</td>
                <td>{item.reason || request.reason || "-"}</td>
                <td>
                  <div className="return-evidence-list compact">
                    {item.evidencePictures.length === 0 && <span>-</span>}
                    {item.evidencePictures.map((picture) => (
                      picture.pictureUrl ? (
                        <a key={picture.pictureId} href={picture.pictureUrl} target="_blank" rel="noreferrer">
                          <img className="evidence-inline-thumb" src={picture.pictureUrl} alt={picture.fileName || "Ảnh hiện trạng"} />
                        </a>
                      ) : (
                        <span key={picture.pictureId}>Ảnh</span>
                      )
                    ))}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="card stack">
        <h2 className="page-title">Ghi chú</h2>
        <div>
          <div className="metric-label">Lý do chung</div>
          <div>{request.reason || "-"}</div>
        </div>
        <div>
          <div className="metric-label">Ngày cửa hàng xử lý</div>
          <div>{shortDate(request.reviewedOn)}</div>
        </div>
      </section>
    </section>
  );
}
