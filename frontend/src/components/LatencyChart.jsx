import { useId } from "react";
import {
  Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis,
} from "recharts";

function LatencyTooltip({ active, payload }) {
  if (!active || !payload?.length) return null;
  const point = payload[0].payload;
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-3 text-sm shadow-lg">
      <p className="font-medium text-slate-900">{point.time}</p>
      <p className="mt-1 text-sky-700">{point.responseTimeMs} ms</p>
      <p className="text-slate-500">HTTP {point.statusCode ?? "—"}</p>
    </div>
  );
}

// data: [{ time: string, responseTimeMs: number, statusCode?: number }]
export default function LatencyChart({ data = [], sourceLabel = "Dữ liệu", endpointName = "Endpoint" }) {
  const gradientId = useId();
  const points = data.filter((point) =>
    Number.isFinite(point.responseTimeMs) && point.responseTimeMs >= 0,
  );
  const values = points.map((point) => point.responseTimeMs);
  const metrics = values.length ? [
    ["MIN", Math.min(...values)],
    ["AVG", values.reduce((sum, value) => sum + value, 0) / values.length],
    ["MAX", Math.max(...values)],
  ] : [["MIN", null], ["AVG", null], ["MAX", null]];

  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm" aria-label="Biểu đồ độ trễ phản hồi">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-lg font-semibold text-slate-900">Độ trễ phản hồi</h2>
          <p className="mt-1 text-sm text-slate-500">{endpointName} · {sourceLabel} · Đơn vị ms</p>
        </div>
        <div className="flex flex-wrap gap-2">
          {metrics.map(([label, value]) => (
            <div key={label} className="rounded-lg bg-sky-50 px-3 py-2 text-sm">
              <span className="mr-2 text-xs font-medium text-slate-500">{label}</span>
              <span className="font-semibold text-sky-800">{value === null ? "—" : `${Number(value.toFixed(1))} ms`}</span>
            </div>
          ))}
        </div>
      </div>
      {points.length ? (
        <div className="mt-6 h-72 w-full min-w-0" role="img" aria-label={`Độ trễ ${points.length} lượt kiểm tra; thấp nhất ${Math.min(...values)} ms, cao nhất ${Math.max(...values)} ms`}>
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={points} margin={{ top: 10, right: 12, bottom: 10, left: 0 }}>
              <defs>
                <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#0ea5e9" stopOpacity={0.3} />
                  <stop offset="100%" stopColor="#0ea5e9" stopOpacity={0.02} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
              <XAxis dataKey="time" tick={{ fontSize: 12 }} tickLine={false} axisLine={false} minTickGap={24} />
              <YAxis unit=" ms" tick={{ fontSize: 12 }} tickLine={false} axisLine={false} width={70} />
              <Tooltip content={<LatencyTooltip />} />
              <Area type="monotone" dataKey="responseTimeMs" stroke="#0ea5e9" strokeWidth={2} fill={`url(#${gradientId})`} activeDot={{ r: 5 }} />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      ) : (
        <p className="flex h-72 items-center justify-center text-sm text-slate-500">Chưa có dữ liệu độ trễ.</p>
      )}
      <p className="mt-3 text-xs text-slate-500">MIN / AVG / MAX tính trên các lượt kiểm tra đang hiển thị.</p>
    </section>
  );
}
