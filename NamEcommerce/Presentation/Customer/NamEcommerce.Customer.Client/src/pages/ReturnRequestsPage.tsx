import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { ReturnRequestSummary } from "../api/types";
import { quantity, returnRequestStatusText, shortDate } from "../app/format";
import { navigate } from "../app/routes";

export function ReturnRequestsPage() {
  const [requests, setRequests] = useState<ReturnRequestSummary[]>([]);

  useEffect(() => {
    apiFetch<{ items: ReturnRequestSummary[] }>("/api/return-requests")
      .then((result) => setRequests(result.items))
      .catch(() => setRequests([]));
  }, []);

  const activeRequests = requests.filter((request) => request.status === 0 || request.status === 1);

  return (
    <section className="stack">
      <div className="toolbar">
        <div>
          <h1 className="page-title">Trả hàng</h1>
          <p className="page-subtitle">Theo dõi các yêu cầu trả hàng đã gửi cho cửa hàng</p>
        </div>
        <button className="button primary" type="button" onClick={() => navigate("/return-requests/new")}>
          Tạo yêu cầu trả hàng
        </button>
      </div>

      {activeRequests.length > 0 && (
        <div className="notice success">
          Có {activeRequests.length} yêu cầu đang được cửa hàng xử lý.
        </div>
      )}

      <section className="card stack">
        <table className="table">
          <thead>
            <tr>
              <th>Phiếu giao</th>
              <th>Trạng thái</th>
              <th>Hàng trả</th>
              <th>Ngày gửi</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {requests.length === 0 && (
              <tr>
                <td colSpan={5}>Chưa có yêu cầu trả hàng.</td>
              </tr>
            )}
            {requests.map((request) => (
              <tr key={request.id}>
                <td>{request.deliveryNoteCode || "-"}</td>
                <td>
                  <span className="badge">{returnRequestStatusText(request.status)}</span>
                </td>
                <td>
                  {request.itemCount} dòng · {quantity(request.totalRequestedQuantity)}
                </td>
                <td>{shortDate(request.createdOn)}</td>
                <td>
                  <button className="button" type="button" onClick={() => navigate(`/return-requests/${request.id}`)}>
                    Xem
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </section>
  );
}
