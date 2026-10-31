import { TrendingUp, AlertTriangle, Globe, Timer } from "lucide-react";

function StatCard({ icon: Icon, label, value, tone }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center justify-between">
        <p className="text-sm text-slate-500">{label}</p>
        <Icon className={`h-5 w-5 ${tone}`} />
      </div>
      <p className="mt-2 text-3xl font-semibold text-slate-900">{value}</p>
    </div>
  );
}

export default function StatsOverview({ stats }) {
  return (
    <section className="mx-auto grid max-w-6xl grid-cols-1 gap-4 px-4 py-6 sm:grid-cols-2 lg:grid-cols-4">
      <StatCard icon={TrendingUp} label="Tỷ lệ Uptime" value={`${stats.uptimePercent}%`} tone="text-emerald-600" />
      <StatCard icon={AlertTriangle} label="Sự cố Critical đang mở" value={stats.openCritical} tone="text-red-500" />
      <StatCard icon={Globe} label="Số lượng Endpoint" value={stats.endpointCount} tone="text-blue-600" />
      <StatCard icon={Timer} label="Độ trễ trung bình p95" value={`${stats.p95Ms} ms`} tone="text-amber-500" />
    </section>
  );
}