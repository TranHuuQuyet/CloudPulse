import { useState } from "react";
import Navbar from "./components/Navbar";
import StatsOverview from "./components/StatsOverview";
import { mockStats, mockMeta } from "./data/mockData";

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
    </div>
  );
}