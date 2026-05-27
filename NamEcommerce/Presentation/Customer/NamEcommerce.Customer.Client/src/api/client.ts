const API_BASE_URL = import.meta.env.VITE_CUSTOMER_API_URL ?? "https://localhost:7001";

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");

  const method = init.method?.toUpperCase() ?? "GET";
  if (method !== "GET") {
    headers.set("Content-Type", "application/json");
    headers.set("X-Customer-Portal-Request", "1");
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    method,
    headers,
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error(await readErrorMessage(response));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

async function readErrorMessage(response: Response) {
  const fallback = `Request failed with status ${response.status}`;
  const contentType = response.headers.get("Content-Type") ?? "";

  if (contentType.includes("application/json") || contentType.includes("+json")) {
    try {
      const body = (await response.json()) as { message?: string; title?: string; detail?: string };
      return body.message || body.detail || body.title || fallback;
    } catch {
      return fallback;
    }
  }

  try {
    const text = await response.text();
    return text || fallback;
  } catch {
    return fallback;
  }
}
