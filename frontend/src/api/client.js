import { mockLatencyData, mockIncidents } from "../data/mockData";

export const API_BASE = (import.meta.env.VITE_API_BASE_URL || "").trim().replace(/\/+$/, "");
export const sourceLabel = API_BASE ? "Dữ liệu API" : "Dữ liệu giả lập";

async function request(path, options) {
  // Vite forwards local /api requests to AWS; production calls the configured API.
  const base = import.meta.env.DEV ? "" : API_BASE;
  const response = await fetch(`${base}${path}`, { ...options, signal: AbortSignal.timeout(20000) });
  if (!response.ok) throw new Error(`API ${response.status}: ${await response.text()}`);
  return response.status === 204 ? null : response.json();
}

const monitorPath = (id) => `/api/monitors/${encodeURIComponent(id)}`;
const normalizeMonitor = (m) => ({ ...m, id: m.monitorId ?? m.id, latencyMs: m.lastResponseTimeMs ?? m.latencyMs ?? 0 });

export async function getMonitorChecks(id, limit = 30) {
  if (!API_BASE) return mockLatencyData.slice(-limit);
  return request(`${monitorPath(id)}/checks?limit=${limit}`);
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
  if (API_BASE) {
    const items = currentMonitors ?? await getMonitors();
    return {
      uptimePercent: items.length ? Math.round(items.filter((m) => m.status === "UP").length / items.length * 1000) / 10 : 0,
      openCritical: items.filter((m) => m.status === "DOWN").length,
      endpointCount: items.length,
      p95Ms: null,
    };
  }
  await wait(200);
  const up = monitors.filter((m) => m.status === "UP");
  return {
    uptimePercent: monitors.length ? Math.round((up.length / monitors.length) * 1000) / 10 : 0,
    openCritical: monitors.filter((m) => m.status === "DOWN").length,
    endpointCount: monitors.length,
    p95Ms: 312,
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
    const result = await request(`${monitorPath(id)}/check`, { method: "POST" });
    return { ...result, latencyMs: result.responseTimeMs, status: result.status ?? result.errorType };
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
