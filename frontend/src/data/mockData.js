export const mockStats = {
  uptimePercent: 99.2,
  openCritical: 1,
  endpointCount: 8,
  p95Ms: 312,
};
export const mockMeta = { region: "ap-southeast-1", cron: "rate(1m)" };

// Relative to page load so the OPEN demo always has a meaningful live duration.
const demoNow = Date.now();
export const mockIncidents = [
  {
    id: "demo-open", endpointName: "Payment API", status: "OPEN",
    startedAt: new Date(demoNow - 185000).toISOString(), resolvedAt: null,
    cause: "HTTP 500 Internal Server Error",
  },
  {
    id: "demo-timeout", endpointName: "Storefront", status: "RESOLVED",
    startedAt: new Date(demoNow - 3600000).toISOString(),
    resolvedAt: new Date(demoNow - 3476000).toISOString(),
    cause: "Request Timeout > 5000ms",
  },
  {
    id: "demo-gateway", endpointName: "Customer API", status: "RESOLVED",
    startedAt: new Date(demoNow - 7200000).toISOString(),
    resolvedAt: new Date(demoNow - 6900000).toISOString(),
    cause: "HTTP 502 Bad Gateway",
  },
];

// Dữ liệu giả của một endpoint; thay bằng kết quả API khi tích hợp backend.
export const mockLatencyData = [
  { time: "09:00", responseTimeMs: 120, statusCode: 200 },
  { time: "09:01", responseTimeMs: 145, statusCode: 200 },
  { time: "09:02", responseTimeMs: 132, statusCode: 200 },
  { time: "09:03", responseTimeMs: 190, statusCode: 200 },
  { time: "09:04", responseTimeMs: 260, statusCode: 500 },
  { time: "09:05", responseTimeMs: 210, statusCode: 500 },
  { time: "09:06", responseTimeMs: 155, statusCode: 200 },
  { time: "09:07", responseTimeMs: 138, statusCode: 200 },
  { time: "09:08", responseTimeMs: 125, statusCode: 200 },
  { time: "09:09", responseTimeMs: 148, statusCode: 200 },
];
