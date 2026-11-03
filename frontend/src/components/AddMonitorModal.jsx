import { useState } from "react";
import { X, AlertCircle } from "lucide-react";
import { MOCK_BASE } from "../api/client";
import { validateUrl } from "../utils/validateUrl";

const PRESETS = [
  { label: "Mock 200 (Bình thường)", name: "Mock 200", url: MOCK_BASE },
  { label: "Mock 500 (Lỗi Server)", name: "Mock 500", url: `${MOCK_BASE}?status=500` },
  { label: "Mock Timeout (Treo 7s)", name: "Mock Timeout", url: `${MOCK_BASE}?delay=7000` },
];

export default function AddMonitorModal({ onClose, onSubmit }) {
  const [name, setName] = useState("");
  const [url, setUrl] = useState("");
  const [saving, setSaving] = useState(false);

  const error = validateUrl(url); // kiểm tra tức thời mỗi lần gõ
  const canSubmit = name.trim() && url.trim() && !error && !saving;

  const applyPreset = (p) => {
    setUrl(p.url);
    if (!name.trim()) setName(p.name);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!canSubmit) return;
    setSaving(true);
    try {
      await onSubmit({ name: name.trim(), url: url.trim() });
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-30 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-900">Thêm Monitor</h2>
          <button onClick={onClose} aria-label="Đóng" className="text-slate-400 hover:text-slate-600">
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="mb-4 flex flex-wrap gap-2">
          {PRESETS.map((p) => (
            <button
              key={p.label}
              type="button"
              onClick={() => applyPreset(p)}
              className="rounded-full border border-slate-200 px-3 py-1 text-xs text-slate-700 hover:bg-slate-50"
            >
              {p.label}
            </button>
          ))}
        </div>

        <div className="grid gap-3">
          <label className="grid gap-1 text-sm text-slate-600">
            Tên website
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Google Search Engine"
              className="rounded-lg border border-slate-200 px-3 py-2 text-slate-900 outline-none focus:border-slate-400"
            />
          </label>

          <label className="grid gap-1 text-sm text-slate-600">
            URL
            <input
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              placeholder="https://www.google.com"
              className={`rounded-lg border px-3 py-2 text-slate-900 outline-none ${
                error ? "border-red-400 focus:border-red-500" : "border-slate-200 focus:border-slate-400"
              }`}
            />
          </label>

          {error && (
            <p className="flex items-start gap-1.5 rounded-lg bg-red-50 p-2.5 text-sm text-red-700">
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
              {error}
            </p>
          )}
        </div>

        <div className="mt-5 flex justify-end gap-2">
          <button onClick={onClose} className="rounded-lg border border-slate-200 px-4 py-2 text-sm hover:bg-slate-50">
            Hủy
          </button>
          <button
            onClick={handleSubmit}
            disabled={!canSubmit}
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white hover:bg-slate-700 disabled:opacity-40"
          >
            {saving ? "Đang lưu..." : "Lưu"}
          </button>
        </div>
      </div>
    </div>
  );
}