import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { ContactInfo, WarehouseContact } from "../api/types";

export function ContactPage() {
  const [contact, setContact] = useState<ContactInfo | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    apiFetch<ContactInfo>("/api/contact")
      .then((result) => {
        setContact(result);
        setError("");
      })
      .catch(() => {
        setContact(null);
        setError("Không thể tải thông tin liên hệ.");
      });
  }, []);

  if (error) return <div className="notice">{error}</div>;
  if (!contact) return <div>Đang tải...</div>;

  return (
    <section className="stack">
      <div>
        <h1 className="page-title">Liên hệ</h1>
        <p className="page-subtitle">Cửa hàng và kho hàng</p>
      </div>
      <section className="contact-layout">
        <div className="card stack">
          <div>
            <div className="metric-label">Cửa hàng</div>
            <h2 className="page-title">{contact.store.storeName}</h2>
          </div>
          <ContactLine label="Số điện thoại" value={contact.store.phoneNumber} />
          <ContactLine label="Email" value={contact.store.email} />
          <ContactLine label="Địa chỉ" value={contact.store.address} />
          <ContactActions
            phoneNumber={contact.store.phoneNumber}
            email={contact.store.email}
            mapQuery={contact.store.mapQuery}
          />
          {contact.store.mapQuery && <MapFrame query={contact.store.mapQuery} title={contact.store.storeName} />}
        </div>
        <div className="stack">
          {contact.warehouses.length === 0 && <div className="notice">Chưa có kho hàng đang hoạt động.</div>}
          {contact.warehouses.map((warehouse) => (
            <WarehouseCard key={warehouse.id} warehouse={warehouse} />
          ))}
        </div>
      </section>
    </section>
  );
}

function WarehouseCard({ warehouse }: { warehouse: WarehouseContact }) {
  return (
    <article className="card stack">
      <div>
        <div className="metric-label">Kho hàng</div>
        <h2 className="page-title">{warehouse.name}</h2>
      </div>
      <ContactLine label="Số điện thoại" value={warehouse.phoneNumber} />
      <ContactLine label="Địa chỉ" value={warehouse.address} />
      <ContactActions phoneNumber={warehouse.phoneNumber} mapQuery={warehouse.mapQuery} />
      {warehouse.mapQuery && <MapFrame query={warehouse.mapQuery} title={warehouse.name} />}
    </article>
  );
}

function ContactLine({ label, value }: { label: string; value?: string | null }) {
  return (
    <div>
      <div className="metric-label">{label}</div>
      <div>{value || "-"}</div>
    </div>
  );
}

function ContactActions({
  phoneNumber,
  email,
  mapQuery,
}: {
  phoneNumber?: string | null;
  email?: string | null;
  mapQuery?: string | null;
}) {
  if (!phoneNumber && !email && !mapQuery) return null;

  return (
    <div className="contact-actions">
      {phoneNumber && (
        <a className="button" href={`tel:${phoneNumber}`}>
          Gọi
        </a>
      )}
      {email && (
        <a className="button" href={`mailto:${email}`}>
          Email
        </a>
      )}
      {mapQuery && (
        <a className="button primary" href={mapSearchUrl(mapQuery)} target="_blank" rel="noreferrer">
          Mở bản đồ
        </a>
      )}
    </div>
  );
}

function MapFrame({ query, title }: { query: string; title: string }) {
  return (
    <iframe
      className="map-frame"
      title={`Bản đồ ${title}`}
      loading="lazy"
      src={`https://www.google.com/maps?q=${encodeURIComponent(query)}&output=embed`}
    />
  );
}

function mapSearchUrl(query: string) {
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(query)}`;
}
