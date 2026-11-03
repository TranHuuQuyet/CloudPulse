export default function Toast({ toast }) {
  if (!toast) return null;
  const style = toast.type === "error" ? "bg-red-600" : "bg-emerald-600";
  return (
    <div className={`fixed bottom-4 right-4 z-40 rounded-lg px-4 py-2 text-sm text-white shadow-lg ${style}`}>
      {toast.message}
    </div>
  );
}