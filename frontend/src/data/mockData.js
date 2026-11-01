export const mockStats = {
  uptimePercent: 99.2,
  openCritical: 1,
  endpointCount: 8,
  p95Ms: 312,
};
export const mockMeta = { region: "ap-southeast-1", cron: "rate(1m)" };

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
