import { FormEvent, useState } from "react";
import { apiFetch } from "../api/client";
import type { CustomerSession } from "../api/types";
import { navigate } from "../app/routes";
import { useAuth } from "../auth/useAuth";

export function LoginPage() {
  const [login, setLogin] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const { refresh } = useAuth();

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError("");
    try {
      await apiFetch<CustomerSession>("/api/auth/password/login", {
        method: "POST",
        body: JSON.stringify({ login, password }),
      });
      await refresh();
      navigate("/app");
    } catch {
      setError("Không thể đăng nhập.");
    }
  }

  return (
    <main className="auth-wrap">
      <form className="auth-card card" onSubmit={submit}>
        <h1 className="page-title">Đăng nhập</h1>
        <p className="page-subtitle">VLXD Tuấn Khôi</p>
        {error && <div className="notice">{error}</div>}
        <div className="field">
          <label>Số điện thoại hoặc email</label>
          <input value={login} onChange={(event) => setLogin(event.target.value)} />
        </div>
        <div className="field">
          <label>Mật khẩu</label>
          <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
        </div>
        <button className="button primary" type="submit">
          Đăng nhập
        </button>
      </form>
    </main>
  );
}
