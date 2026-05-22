import { FormEvent, useState } from "react";
import { apiFetch } from "../api/client";
import type { ActionResult } from "../api/types";
import { useAuth } from "../auth/useAuth";

type MessageState = {
  text: string;
  tone: "success" | "error";
};

export function SetPasswordPage() {
  const { session, refresh } = useAuth();
  const [currentPassword, setCurrentPassword] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [message, setMessage] = useState<MessageState | null>(null);
  const hasPassword = session?.hasPassword ?? false;

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (password !== confirmPassword) {
      setMessage({ text: "Mật khẩu xác nhận không khớp.", tone: "error" });
      return;
    }

    try {
      const result = await apiFetch<ActionResult>(hasPassword ? "/api/auth/password/change" : "/api/auth/password/set", {
        method: "POST",
        body: JSON.stringify(hasPassword ? { currentPassword, newPassword: password } : { password }),
      });
      setMessage({
        text: result.message ?? (result.success ? "Đã lưu mật khẩu." : "Không thể lưu mật khẩu."),
        tone: result.success ? "success" : "error",
      });
      setPassword("");
      setConfirmPassword("");
      setCurrentPassword("");
      await refresh();
    } catch {
      setMessage({ text: "Không thể lưu mật khẩu.", tone: "error" });
    }
  }

  return (
    <section className="card">
      <h1 className="page-title">{hasPassword ? "Đổi mật khẩu" : "Cài đặt mật khẩu"}</h1>
      <p className="page-subtitle">VLXD Tuấn Khôi</p>
      {message && <div className={message.tone === "error" ? "notice" : "badge"}>{message.text}</div>}
      <form onSubmit={submit}>
        {hasPassword && (
          <div className="field">
            <label>Mật khẩu hiện tại</label>
            <input type="password" value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} />
          </div>
        )}
        <div className="field">
          <label>Mật khẩu mới</label>
          <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
        </div>
        <div className="field">
          <label>Xác nhận mật khẩu mới</label>
          <input type="password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} />
        </div>
        <button className="button primary" type="submit">
          {hasPassword ? "Đổi mật khẩu" : "Lưu mật khẩu"}
        </button>
      </form>
    </section>
  );
}
