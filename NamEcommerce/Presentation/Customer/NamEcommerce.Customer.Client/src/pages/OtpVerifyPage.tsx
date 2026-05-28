import { FormEvent, useState } from "react";
import { apiFetch } from "../api/client";
import type { CustomerSession } from "../api/types";
import { navigate } from "../app/routes";
import { useAuth } from "../auth/useAuth";

export function OtpVerifyPage({
  challengeId,
  mockOtp,
  returnUrl,
}: {
  challengeId: string;
  mockOtp?: string | null;
  returnUrl?: string | null;
}) {
  const [otp, setOtp] = useState(mockOtp ?? "");
  const [error, setError] = useState("");
  const { refresh } = useAuth();

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError("");
    try {
      await apiFetch<CustomerSession>("/api/auth/otp/verify", {
        method: "POST",
        body: JSON.stringify({ challengeId, otp }),
      });
      await refresh();
      navigate(safeReturnUrl(returnUrl));
    } catch {
      setError("Mã OTP không hợp lệ.");
    }
  }

  return (
    <main className="auth-wrap">
      <form className="auth-card card" onSubmit={submit}>
        <h1 className="page-title">Xác thực OTP</h1>
        <p className="page-subtitle">VLXD Tuấn Khôi</p>
        {error && <div className="notice">{error}</div>}
        <div className="field">
          <label>Mã OTP</label>
          <input value={otp} inputMode="numeric" onChange={(event) => setOtp(event.target.value)} />
        </div>
        <button className="button primary" type="submit">
          Xác nhận
        </button>
      </form>
    </main>
  );
}

function safeReturnUrl(returnUrl?: string | null) {
  if (!returnUrl || !returnUrl.startsWith("/") || returnUrl.startsWith("//")) {
    return "/app";
  }

  return returnUrl;
}
