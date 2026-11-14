import { mockLatencyData, mockIncidents } from "../data/mockData";

export const API_BASE = (import.meta.env.VITE_API_BASE_URL || "").trim().replace(/\/+$/, "");
export const sourceLabel = API_BASE ? "Dữ liệu API" : "Dữ liệu giả lập";

async function request(path, options) {
  // Vite forwards local /api requests to AWS; production calls the configured API.
  const base = import.meta.env.DEV ? "" : API_BASE;
  const response = await fetch(`${base}${path}`, { ...options, signal: AbortSignal.timeout(20000) });
  if (response.status === 204) return null; // No Content: không gọi response.json()
  if (!response.ok) {
    let message = `API ${response.status}`;
    try {
      const data = await response.json();
      if (data?.message) message = data.message; // vd 400 SSRF: hiện thông điệp từ backend
    } catch {
      // phản hồi lỗi không phải JSON: giữ thông báo mặc định
    }
    throw new Error(message);
  }
  return response.json();
}

const monitorPath = (id) => `/api/monitors/${encodeURIComponent(id)}`;
const normalizeMonitor = (m) => ({ ...m, id: m.monitorId ?? m.id, latencyMs: m.lastResponseTimeMs ?? m.latencyMs ?? 0 });

const vietnamTimeFormat = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hourCycle: "h23",
  timeZone: "Asia/Ho_Chi_Minh",
});

export function formatCheckTime(checkedAt, fallbackTime = "—") {
  if (typeof checkedAt !== "string" || !checkedAt.trim()) return fallbackTime;
  const ms = Date.parse(checkedAt);
  if (!Number.isFinite(ms)) return fallbackTime;

  const parts = Object.fromEntries(
    vietnamTimeFormat.formatToParts(ms).map((part) => [part.type, part.value]),
  );
  return `${parts.day}/${parts.month} ${parts.hour}:${parts.minute}:${parts.second}`;
}

export function normalizeCheckPoint(point) {
  return {
    ...point,
    time: formatCheckTime(point?.checkedAt, point?.time ?? "—"),
  };
}

export async function getMonitorChecks(id, limit = 30) {
  if (!API_BASE) return mockLatencyData.slice(-limit).map(normalizeCheckPoint);
  const items = await request(`${monitorPath(id)}/checks?limit=${limit}`);
  return Array.isArray(items) ? items.map(normalizeCheckPoint) : [];
}

export async function getMonitorIncidents(id, endpointName, limit = 10) {
  if (!API_BASE) return mockIncidents.slice(0, limit).map((item) => ({ ...item, endpointName }));
  const items = await request(`${monitorPath(id)}/incidents?limit=${limit}`);
  return items.map((item) => ({
    id: item.incidentId, endpointName, status: item.status,
    startedAt: item.openedAt, resolvedAt: item.resolvedAt,
    cause: item.reason, downtimeSeconds: item.downtimeSeconds,
  }));
}

// Local fallback is used only when API_BASE is not configured.
const wait = (ms) => new Promise((r) => setTimeout(r, ms));

// Configure the deployed demo target with VITE_MOCK_URL.
export const MOCK_BASE =
  import.meta.env.VITE_MOCK_URL || "https://example.execute-api.amazonaws.com/mock";

let monitors = [
  { id: "1", name: "Mock 200", url: `${MOCK_BASE}`, status: "UP", consecutiveFailures: 0, latencyMs: 52 },
  { id: "2", name: "Google", url: "https://www.google.com", status: "UP", consecutiveFailures: 0, latencyMs: 87 },
  { id: "3", name: "Mock 500", url: `${MOCK_BASE}?status=500`, status: "DOWN", consecutiveFailures: 2, latencyMs: 0 },
];

export async function getMonitors() {
  if (API_BASE) return (await request("/api/monitors")).map(normalizeMonitor);
  await wait(300);
  return monitors.map((m) => ({ ...m }));
}

export async function getStats(currentMonitors) {
  const items = currentMonitors ?? (await getMonitors());
  const checked = items.filter((m) => m.status === "UP" || m.status === "DOWN");
  const up = checked.filter((m) => m.status === "UP").length;
  const latencies = items.map((m) => m.latencyMs).filter((v) => v > 0).sort((a, b) => a - b);
  const p95Ms = latencies.length
    ? latencies[Math.max(0, Math.ceil(latencies.length * 0.95) - 1)]
    : null;

  return {
    uptimePercent: checked.length ? Math.round((up / checked.length) * 1000) / 10 : 0,
    openCritical: items.filter((m) => m.status === "DOWN").length,
    endpointCount: items.length,
    p95Ms,
  };
}

export async function addMonitor({ name, url }) {
  if (API_BASE) return normalizeMonitor(await request("/api/monitors", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name, url }) }));
  await wait(300);
  const m = { id: String(Date.now()), name, url, status: "UNKNOWN", consecutiveFailures: 0, latencyMs: 0 };
  monitors = [...monitors, m];
  return m;
}

export async function deleteMonitor(id) {
  if (API_BASE) return request(monitorPath(id), { method: "DELETE" });
  await wait(200);
  monitors = monitors.filter((m) => m.id !== id);
}

// Giả lập Check Now + State Machine: lỗi 2 lần liên tiếp mới chuyển DOWN (AC-04)
export async function checkNow(id) {
  if (API_BASE) {
    const data = await request(`${monitorPath(id)}/check`, { method: "POST" });
    return {
      id: data.monitorId,
      isSuccess: data.isSuccess,
      statusCode: data.statusCode,
      latencyMs: data.responseTimeMs,
      errorType: data.errorType,
      errorMessage: data.errorMessage,
    };
  }
  await wait(1000); // AC-08: phản hồi <= 2 giây
  const m = monitors.find((x) => x.id === id);
  if (!m) throw new Error("Monitor not found");

  const isTimeout = m.url.includes("delay=");
  const isError = m.url.includes("status=5") || m.url.includes("status=4");
  const isSuccess = !isTimeout && !isError;

  if (isSuccess) {
    m.status = "UP";
    m.consecutiveFailures = 0;
    m.latencyMs = 40 + Math.floor(Math.random() * 60);
  } else {
    m.consecutiveFailures += 1;
    m.latencyMs = isTimeout ? 5000 : 0;
    if (m.consecutiveFailures >= 2) m.status = "DOWN";
  }
  return { id, isSuccess, status: m.status, latencyMs: m.latencyMs };
}
