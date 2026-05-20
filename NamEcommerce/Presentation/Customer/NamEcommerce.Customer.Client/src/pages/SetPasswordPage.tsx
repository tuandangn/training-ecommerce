import { FormEvent, useState } from "react";
import { apiFetch } from "../api/client";
import type { ActionResult } from "../api/types";

export function SetPasswordPage() {
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const result = await apiFetch<ActionResult>("/api/auth/password/set", {
      method: "POST",
      body: JSON.stringify({ password }),
    });
    setMessage(result.message ?? (result.success ? "Đã lưu mật khẩu." : "Không thể lưu mật khẩu."));
  }

  return (
    <section className="card">
      <h1 className="page-title">Cài đặt mật khẩu</h1>
      <p className="page-subtitle">VLXD Tuấn Khôi</p>
      {message && <div className={message.includes("Không") ? "notice" : "badge"}>{message}</div>}
      <form onSubmit={submit}>
        <div className="field">
          <label>Mật khẩu mới</label>
          <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
        </div>
        <button className="button primary" type="submit">
          Lưu mật khẩu
        </button>
      </form>
    </section>
  );
}
