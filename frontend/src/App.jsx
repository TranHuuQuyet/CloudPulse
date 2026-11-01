import { useState } from "react";
import Navbar from "./components/Navbar";
import StatsOverview from "./components/StatsOverview";
import { mockStats, mockMeta, mockLatencyData } from "./data/mockData";
import LatencyChart from "./components/LatencyChart";

export default function App() {
  const [syncing, setSyncing] = useState(false);

  const handleSync = () => {
    setSyncing(true);
    setTimeout(() => setSyncing(false), 1500);
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar
        region={mockMeta.region}
        cron={mockMeta.cron}
        syncing={syncing}
        onSync={handleSync}
        onAdd={() => console.log("Mở modal thêm monitor")}
      />
      <StatsOverview stats={mockStats} />
      <main className="mx-auto max-w-6xl px-4 pb-8">
        <LatencyChart data={mockLatencyData} sourceLabel="Dữ liệu giả (Mock Data)" endpointName="MockTarget" />
      </main>
    </div>
  );
}
