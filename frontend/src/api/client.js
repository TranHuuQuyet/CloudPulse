// Mock API cho Tuần 1-2. Tuần 3 thay bằng fetch tới API Gateway thật.
const wait = (ms) => new Promise((r) => setTimeout(r, ms));

// TV1 sẽ cấp URL MockTarget thật sau khi deploy. Tạm dùng placeholder.
export const MOCK_BASE =
  import.meta.env.VITE_MOCK_URL || "https://example.execute-api.amazonaws.com/mock";

let monitors = [
  { id: "1", name: "Mock 200", url: `${MOCK_BASE}`, status: "UP", consecutiveFailures: 0, latencyMs: 52 },
  { id: "2", name: "Google", url: "https://www.google.com", status: "UP", consecutiveFailures: 0, latencyMs: 87 },
  { id: "3", name: "Mock 500", url: `${MOCK_BASE}?status=500`, status: "DOWN", consecutiveFailures: 2, latencyMs: 0 },
];

export async function getMonitors() {
  await wait(300);
  return monitors.map((m) => ({ ...m }));
}

export async function getStats() {
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
  await wait(300);
  const m = { id: String(Date.now()), name, url, status: "UNKNOWN", consecutiveFailures: 0, latencyMs: 0 };
  monitors = [...monitors, m];
  return m;
}

export async function deleteMonitor(id) {
  await wait(200);
  monitors = monitors.filter((m) => m.id !== id);
}

// Giả lập Check Now + State Machine: lỗi 2 lần liên tiếp mới chuyển DOWN (AC-04)
export async function checkNow(id) {
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