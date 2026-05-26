import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { ContactInfo } from "../api/types";
import { AuthProvider } from "../auth/AuthContext";
import { useAuth } from "../auth/useAuth";
import { ContactPage } from "../pages/ContactPage";
import { DashboardPage } from "../pages/DashboardPage";
import { DebtsPage } from "../pages/DebtsPage";
import { DeliveryNoteDetailsPage } from "../pages/DeliveryNoteDetailsPage";
import { DeliveryNotesPage } from "../pages/DeliveryNotesPage";
import { LoginPage } from "../pages/LoginPage";
import { MockPaymentPage } from "../pages/MockPaymentPage";
import { NewOrderRequestPage } from "../pages/NewOrderRequestPage";
import { OrderDetailsPage } from "../pages/OrderDetailsPage";
import { OrderRequestDetailsPage } from "../pages/OrderRequestDetailsPage";
import { OrdersPage } from "../pages/OrdersPage";
import { OtpVerifyPage } from "../pages/OtpVerifyPage";
import { PublicDeliveryPage } from "../pages/PublicDeliveryPage";
import { SetPasswordPage } from "../pages/SetPasswordPage";
import { navigate, useRoute } from "./routes";

export function App() {
  return (
    <AuthProvider>
      <AppRoutes />
    </AuthProvider>
  );
}

function AppRoutes() {
  const route = useRoute();
  const { session, loading, logout } = useAuth();
  const path = window.location.pathname;
  const query = new URLSearchParams(window.location.search);

  if (path.startsWith("/d/") || path.startsWith("/delivery/")) {
    const token = path.startsWith("/d/")
      ? path.replace("/d/", "")
      : path.replace("/delivery/", "");
    return <PublicDeliveryPage token={decodeURIComponent(token)} />;
  }

  if (path === "/verify") {
    return (
      <OtpVerifyPage
        challengeId={query.get("challengeId") ?? ""}
        mockOtp={query.get("mockOtp")}
        returnUrl={query.get("returnUrl")}
      />
    );
  }

  if (path === "/login") {
    return <LoginPage />;
  }

  if (loading) {
    return <main className="auth-wrap">Đang tải...</main>;
  }

  if (!session) {
    return <LoginPage />;
  }

  return (
    <div className="app-shell" data-route={route}>
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark">VK</div>
          <div>
            <div>VLXD Tuấn Khôi</div>
            <div className="page-subtitle">Cổng khách hàng</div>
          </div>
        </div>
        <nav className="nav">
          <NavLink href="/app" label="Tổng quan" />
          <NavLink href="/orders" label="Đơn hàng" />
          <NavLink href="/delivery-notes" label="Phiếu giao" />
          <NavLink href="/debts" label="Công nợ" />
          <NavLink href="/set-password" label="Mật khẩu" />
          <NavLink href="/contact" label="Liên hệ" />
          <button onClick={logout}>Đăng xuất</button>
        </nav>
        <SidebarContact />
        <div className="sidebar-user">{session.customerName}</div>
      </aside>
      <main className="main">{renderPrivatePage(path)}</main>
    </div>
  );
}

function NavLink({ href, label }: { href: string; label: string }) {
  const active = window.location.pathname === href;
  return (
    <a
      className={active ? "active" : ""}
      href={href}
      onClick={(event) => {
        event.preventDefault();
        navigate(href);
      }}
    >
      {label}
    </a>
  );
}

function renderPrivatePage(path: string) {
  if (path === "/app" || path === "/") return <DashboardPage />;
  if (path === "/orders") return <OrdersPage />;
  if (path === "/orders/new") return <NewOrderRequestPage />;
  if (path.startsWith("/order-requests/")) return <OrderRequestDetailsPage id={path.replace("/order-requests/", "")} />;
  if (path.startsWith("/orders/")) return <OrderDetailsPage id={path.replace("/orders/", "")} />;
  if (path === "/delivery-notes") return <DeliveryNotesPage />;
  if (path.startsWith("/delivery-notes/")) return <DeliveryNoteDetailsPage id={path.replace("/delivery-notes/", "")} />;
  if (path === "/debts") return <DebtsPage />;
  if (path === "/payments") return <MockPaymentPage />;
  if (path === "/set-password") return <SetPasswordPage />;
  if (path === "/contact") return <ContactPage />;
  return <DashboardPage />;
}

function SidebarContact() {
  const [contact, setContact] = useState<ContactInfo | null>(null);

  useEffect(() => {
    apiFetch<ContactInfo>("/api/contact").then(setContact).catch(() => setContact(null));
  }, []);

  if (!contact) return null;

  return (
    <div className="sidebar-contact">
      <strong>{contact.store.storeName}</strong>
      <span>{contact.store.phoneNumber || contact.store.email || contact.store.address || "Thông tin liên hệ"}</span>
    </div>
  );
}
