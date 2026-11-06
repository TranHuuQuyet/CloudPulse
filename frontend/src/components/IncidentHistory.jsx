import { useEffect, useState } from "react";

const dateFormat = new Intl.DateTimeFormat("vi-VN", {
  dateStyle: "short", timeStyle: "medium", timeZone: "Asia/Ho_Chi_Minh",
});

function timestamp(value) {
  return value ? Date.parse(value) : NaN;
}

function displayDate(value) {
  return Number.isFinite(timestamp(value)) ? dateFormat.format(timestamp(value)) : "—";
}

// OPEN counts from startedAt to now; RESOLVED freezes at resolvedAt.
function downtime(incident, now) {
  const start = timestamp(incident.startedAt);
  const end = incident.status === "OPEN" ? now : timestamp(incident.resolvedAt);
  return Number.isFinite(start) && Number.isFinite(end) && end >= start
    ? `${Math.floor((end - start) / 1000)}s downtime` : "—";
}

export default function IncidentHistory({ incidents = [], sourceLabel = "Dữ liệu" }) {
  const [now, setNow] = useState(Date.now);
  const hasOpen = incidents.some((incident) => incident.status === "OPEN");

  useEffect(() => {
    if (!hasOpen) return;
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, [hasOpen]);

  const sorted = [...incidents].sort((a, b) =>
    (timestamp(b.startedAt) || 0) - (timestamp(a.startedAt) || 0));

  return (
    <section className="mt-6 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm" aria-labelledby="incident-history-title">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="incident-history-title" className="text-lg font-semibold text-slate-900">Lịch sử sự cố</h2>
          <p className="mt-1 text-sm text-slate-500">{sourceLabel} · Mới nhất trước · Giờ Việt Nam (UTC+7)</p>
        </div>
        <span className="rounded-lg bg-slate-100 px-3 py-2 text-sm text-slate-600">{incidents.length} sự cố · {incidents.filter((item) => item.status === "OPEN").length} đang diễn ra</span>
      </div>
      {sorted.length ? (
        <div className="mt-5 overflow-x-auto">
          <table className="w-full min-w-[760px] text-left text-sm">
            <caption className="sr-only">Thời gian, trạng thái, nguyên nhân và thời lượng gián đoạn của từng endpoint</caption>
            <thead className="border-b border-slate-200 text-xs uppercase text-slate-500">
              <tr>{["Endpoint / Nguyên nhân", "Trạng thái", "Bắt đầu", "Khôi phục", "Gián đoạn"].map((label) => <th key={label} scope="col" className="px-3 py-3 font-medium">{label}</th>)}</tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {sorted.map((incident) => (
                <tr key={incident.id} className="align-top">
                  <td className="px-3 py-4"><p className="font-medium text-slate-900">{incident.endpointName}</p><p className="mt-1 break-all text-xs text-slate-500">{incident.cause || "Chưa xác định nguyên nhân"}</p></td>
                  <td className="px-3 py-4"><span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${incident.status === "OPEN" ? "bg-red-50 text-red-700" : incident.status === "RESOLVED" ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-600"}`}>{incident.status}</span></td>
                  <td className="whitespace-nowrap px-3 py-4 text-slate-600">{displayDate(incident.startedAt)}</td>
                  <td className="whitespace-nowrap px-3 py-4 text-slate-600">{incident.status === "OPEN" ? "Đang diễn ra" : displayDate(incident.resolvedAt)}</td>
                  <td className="whitespace-nowrap px-3 py-4 font-medium text-slate-700">{downtime(incident, now)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : <p className="py-12 text-center text-sm text-slate-500">Chưa có sự cố nào được ghi nhận.</p>}
      <p className="mt-4 text-xs text-slate-500">Downtime tính từ lúc xác nhận DOWN. Sự cố OPEN cập nhật mỗi giây; RESOLVED giữ thời lượng đến lúc khôi phục.</p>
    </section>
  );
}
