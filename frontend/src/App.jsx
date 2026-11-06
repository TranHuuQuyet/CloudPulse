import { useCallback, useEffect, useState } from "react";
import Navbar from "./components/Navbar";
import StatsOverview from "./components/StatsOverview";
import MonitorList from "./components/MonitorList";
import AddMonitorModal from "./components/AddMonitorModal";
import LatencyChart from "./components/LatencyChart";
import IncidentHistory from "./components/IncidentHistory";
import Toast from "./components/Toast";
import { mockMeta, mockLatencyData, mockIncidents } from "./data/mockData";
import { getMonitors, getStats, addMonitor, deleteMonitor, checkNow } from "./api/client";

const EMPTY_STATS = { uptimePercent: 0, openCritical: 0, endpointCount: 0, p95Ms: 0 };

export default function App() {
  const [monitors, setMonitors] = useState([]);
  const [stats, setStats] = useState(EMPTY_STATS);
  const [syncing, setSyncing] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [toast, setToast] = useState(null);

  const showToast = (message, type = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 3000);
  };

  const load = useCallback(async () => {
    const [m, s] = await Promise.all([getMonitors(), getStats()]);
    setMonitors(m);
    setStats(s);
  }, []);

    useEffect(() => {
    let cancelled = false;
    (async () => {
      const [m, s] = await Promise.all([getMonitors(), getStats()]);
      if (!cancelled) {
        setMonitors(m);
        setStats(s);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const handleSync = async () => {
    setSyncing(true);
    await load();
    setSyncing(false);
  };

  const handleAdd = async (data) => {
    await addMonitor(data);
    await load();
    setShowModal(false);
    showToast(`Đã thêm "${data.name}"`);
  };

  const handleDelete = async (id) => {
    await deleteMonitor(id);
    await load();
    showToast("Đã xóa monitor");
  };

  const handleCheck = async (id) => {
    const r = await checkNow(id);
    await load();
    showToast(
      r.isSuccess ? `Kiểm tra OK: ${r.latencyMs} ms` : `Kiểm tra thất bại (${r.status})`,
      r.isSuccess ? "success" : "error"
    );
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

      {/* Phần của TV5: giữ nguyên */}
      <main className="mx-auto max-w-6xl px-4 pb-8">
        <LatencyChart data={mockLatencyData} sourceLabel="Dữ liệu giả lập" />
        <IncidentHistory incidents={mockIncidents} sourceLabel="Dữ liệu giả lập" />
      </main>

      {showModal && <AddMonitorModal onClose={() => setShowModal(false)} onSubmit={handleAdd} />}
      <Toast toast={toast} />
    </div>
  );
}
