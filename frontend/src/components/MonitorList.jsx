import { useState } from "react";
import { Search, RefreshCw, Trash2 } from "lucide-react";

const TABS = ["ALL", "UP", "DOWN"];

function RadarPing({ status }) {
  const color =
    status === "UP" ? "bg-emerald-500" : status === "DOWN" ? "bg-red-500" : "bg-slate-400";
  return (
    <span className="relative flex h-3 w-3">
      {status !== "UNKNOWN" && (
        <span className={`absolute inline-flex h-full w-full animate-ping rounded-full opacity-75 ${color}`} />
      )}
      <span className={`relative inline-flex h-3 w-3 rounded-full ${color}`} />
    </span>
  );
}

function StatusBadge({ status }) {
  const style =
    status === "UP"
      ? "bg-emerald-50 text-emerald-700"
      : status === "DOWN"
      ? "bg-red-50 text-red-700"
      : "bg-slate-100 text-slate-600";
  return <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${style}`}>{status}</span>;
}

export default function MonitorList({ monitors, onCheck, onDelete }) {
  const [query, setQuery] = useState("");
  const [tab, setTab] = useState("ALL");
  const [checkingIds, setCheckingIds] = useState([]);

  const q = query.trim().toLowerCase();
  const visible = monitors.filter((m) => {
    const matchTab = tab === "ALL" || m.status === tab;
    const matchQuery = !q || m.name.toLowerCase().includes(q) || m.url.toLowerCase().includes(q);
    return matchTab && matchQuery;
  });

  const count = (t) => (t === "ALL" ? monitors.length : monitors.filter((m) => m.status === t).length);

  const handleCheck = async (id) => {
    setCheckingIds((ids) => [...ids, id]);
    try {
      await onCheck(id);
    } finally {
      setCheckingIds((ids) => ids.filter((x) => x !== id));
    }
  };

  const handleDelete = (m) => {
    if (window.confirm(`Xóa "${m.name}" khỏi danh sách theo dõi?`)) onDelete(m.id);
  };

  return (
    <section className="mx-auto max-w-6xl px-4 pb-10">
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative sm:w-80">
          <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Tìm theo tên hoặc URL..."
            className="w-full rounded-lg border border-slate-200 bg-white py-2 pl-9 pr-3 text-sm outline-none focus:border-slate-400"
          />
        </div>

        <div className="flex gap-1 rounded-lg bg-slate-100 p-1">
          {TABS.map((t) => (
            <button
              key={t}
              onClick={() => setTab(t)}
              className={`rounded-md px-3 py-1 text-sm ${
                tab === t ? "bg-white font-medium text-slate-900 shadow-sm" : "text-slate-500"
              }`}
            >
              {t} ({count(t)})
            </button>
          ))}
        </div>
      </div>

      {visible.length === 0 ? (
        <p className="rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center text-sm text-slate-500">
          Không có monitor nào phù hợp.
        </p>
      ) : (
        <ul className="grid gap-3">
          {visible.map((m) => {
            const checking = checkingIds.includes(m.id);
            return (
              <li
                key={m.id}
                className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center sm:justify-between"
              >
                <div className="flex min-w-0 items-center gap-3">
                  <RadarPing status={m.status} />
                  <div className="min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="truncate font-medium text-slate-900">{m.name}</p>
                      <StatusBadge status={m.status} />
                    </div>
                    <p className="truncate text-sm text-slate-500">{m.url}</p>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  {m.latencyMs > 0 && (
                    <span className="mr-2 font-mono text-xs text-slate-500">{m.latencyMs} ms</span>
                  )}
                  <button
                    onClick={() => handleCheck(m.id)}
                    disabled={checking}
                    className="flex items-center gap-1 rounded-lg border border-slate-200 px-3 py-1.5 text-sm hover:bg-slate-50 disabled:opacity-60"
                  >
                    <RefreshCw className={`h-4 w-4 ${checking ? "animate-spin" : ""}`} />
                    Check Now
                  </button>
                  <button
                    onClick={() => handleDelete(m)}
                    aria-label={`Xóa ${m.name}`}
                    className="rounded-lg border border-slate-200 p-2 text-slate-500 hover:bg-red-50 hover:text-red-600"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}