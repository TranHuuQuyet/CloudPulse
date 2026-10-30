import { useEffect, useState } from "react";
import { RefreshCw, Plus, Activity } from "lucide-react";

export default function Navbar({ region, cron, onSync, onAdd, syncing }) {
  const [now, setNow] = useState(new Date());

  useEffect(() => {
    const t = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(t);
  }, []);

  const utc = now.toISOString().slice(11, 19);

  return (
    <nav className="sticky top-0 z-20 border-b border-slate-200 bg-white/80 backdrop-blur">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3">
        <div className="flex items-center gap-2">
          <Activity className="h-6 w-6 text-emerald-600" />
          <span className="text-lg font-bold text-slate-900">CloudPulse</span>
        </div>

        <div className="flex flex-wrap items-center gap-2 text-xs">
          <span className="rounded-full bg-slate-100 px-3 py-1 font-mono text-slate-700">
            UTC {utc}
          </span>
          <span className="rounded-full bg-slate-100 px-3 py-1 text-slate-700">{region}</span>
          <span className="rounded-full bg-slate-100 px-3 py-1 font-mono text-slate-700">{cron}</span>

          <button
            onClick={onSync}
            disabled={syncing}
            className="flex items-center gap-1 rounded-lg border border-slate-200 px-3 py-1.5 text-sm hover:bg-slate-50 disabled:opacity-60"
          >
            <RefreshCw className={`h-4 w-4 ${syncing ? "animate-spin" : ""}`} />
            Sync
          </button>
          <button
            onClick={onAdd}
            className="flex items-center gap-1 rounded-lg bg-slate-900 px-3 py-1.5 text-sm text-white hover:bg-slate-700"
          >
            <Plus className="h-4 w-4" />
            Thêm Monitor
          </button>
        </div>
      </div>
    </nav>
  );
}