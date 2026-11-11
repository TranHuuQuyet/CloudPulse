import { useCallback, useEffect, useState } from "react";
import Navbar from "./components/Navbar";
import StatsOverview from "./components/StatsOverview";
import MonitorList from "./components/MonitorList";
import AddMonitorModal from "./components/AddMonitorModal";
import LatencyChart from "./components/LatencyChart";
import IncidentHistory from "./components/IncidentHistory";
import Toast from "./components/Toast";
import { mockMeta } from "./data/mockData";
import { getMonitors, getStats, addMonitor, deleteMonitor, checkNow, getMonitorChecks, getMonitorIncidents, sourceLabel } from "./api/client";

const EMPTY_STATS = { uptimePercent: 0, openCritical: 0, endpointCount: 0, p95Ms: null };

export default function App() {
  const [monitors, setMonitors] = useState([]);
  const [selectedMonitorId, setSelectedMonitorId] = useState("");
  const [revision, setRevision] = useState(0);
  const [history, setHistory] = useState({ checks: [], incidents: [], loading: false, error: null });
  const [loadError, setLoadError] = useState(null);
  const selectedMonitor = monitors.find((m) => m.id === selectedMonitorId) ?? monitors[0];
  const monitorId = selectedMonitor?.id;
  const monitorName = selectedMonitor?.name;
  const [stats, setStats] = useState(EMPTY_STATS);
  const [syncing, setSyncing] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [toast, setToast] = useState(null);

  const showToast = (message, type = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 3000);
  };

  const load = useCallback(async () => {
    const m = await getMonitors();
    const s = await getStats(m);
    setMonitors(m);
    setStats(s);
    setLoadError(null);
    setRevision((value) => value + 1);
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const m = await getMonitors();
        const s = await getStats(m);
        if (!cancelled) { setMonitors(m); setStats(s); }
      } catch (error) {
        if (!cancelled) setLoadError(error.message);
      }
    })();
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setHistory({ checks: [], incidents: [], loading: Boolean(monitorId), error: null });
      if (!monitorId) return;
      try {
        const [checks, incidents] = await Promise.all([
          getMonitorChecks(monitorId), getMonitorIncidents(monitorId, monitorName),
        ]);
        if (!cancelled) setHistory({ checks, incidents, loading: false, error: null });
      } catch (error) {
        if (!cancelled) setHistory({ checks: [], incidents: [], loading: false, error: error.message });
      }
    })();
    return () => { cancelled = true; };
  }, [monitorId, monitorName, revision]);

  const handleSync = async () => {
    setSyncing(true);
    try { await load(); }
    catch (error) { setLoadError(error.message); }
    finally { setSyncing(false); }
  };

  const handleAdd = async (data) => {
    try {
      await addMonitor(data);
      setShowModal(false);
      await load();
      showToast(`Đã thêm "${data.name}"`);
    } catch (error) { showToast(error.message, "error"); }
  };

  const handleDelete = async (id) => {
    try {
      await deleteMonitor(id);
      await load();
      showToast("Đã xóa monitor");
    } catch (error) { showToast(error.message, "error"); }
  };

  const handleCheck = async (id) => {
    try {
      const r = await checkNow(id);
      await load();
      showToast(
        r.isSuccess ? `Kiểm tra OK: ${r.latencyMs} ms` : `Kiểm tra thất bại (${r.status})`,
        r.isSuccess ? "success" : "error"
      );
    } catch (error) { showToast(error.message, "error"); }
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar
        region={mockMeta.region}
        cron={mockMeta.cron}
        syncing={syncing}
        onSync={handleSync}
        onAdd={() => setShowModal(true)}
      />
      <StatsOverview stats={stats} />
      <MonitorList monitors={monitors} onCheck={handleCheck} onDelete={handleDelete} />

      <main className="mx-auto max-w-6xl px-4 pb-8">
        {loadError && <p role="alert" className="mb-4 text-red-700">{loadError}</p>}
        <label className="mb-4 block text-sm text-slate-700">
          Monitor đang xem
          <select value={monitorId ?? ""} onChange={(event) => setSelectedMonitorId(event.target.value)} className="ml-3 rounded-lg border p-2" disabled={!monitors.length}>
            {!monitors.length && <option value="">Chưa có monitor</option>}
            {monitors.map((m) => <option key={m.id} value={m.id}>{m.name}</option>)}
          </select>
        </label>
        {history.loading ? <p role="status">Đang tải lịch sử…</p> : history.error ? (
          <p role="alert" className="text-red-700">Không tải được lịch sử: {history.error}. Bấm Sync để thử lại.</p>
        ) : (
          <>
            <LatencyChart data={history.checks} endpointName={selectedMonitor?.name} sourceLabel={sourceLabel} />
            <IncidentHistory incidents={history.incidents} endpointName={selectedMonitor?.name} sourceLabel={sourceLabel} />
          </>
        )}
      </main>

      {showModal && <AddMonitorModal onClose={() => setShowModal(false)} onSubmit={handleAdd} />}
      <Toast toast={toast} />
    </div>
  );
}
