const BLOCKED_MESSAGE = "Private or loopback IP addresses are not permitted.";

function isPrivateIPv4(host) {
  const m = host.match(/^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$/);
  if (!m) return false;
  const [a, b] = [Number(m[1]), Number(m[2])];
  return (
    a === 0 ||
    a === 10 ||
    a === 127 ||
    (a === 169 && b === 254) ||
    (a === 172 && b >= 16 && b <= 31) ||
    (a === 192 && b === 168)
  );
}

// Trả về thông báo lỗi, hoặc null nếu URL hợp lệ
export function validateUrl(raw) {
  const value = raw.trim();
  if (!value) return null;

  let url;
  try {
    url = new URL(value);
  } catch {
    return "URL không hợp lệ (ví dụ: https://example.com).";
  }

  if (url.protocol !== "http:" && url.protocol !== "https:") {
    return "Chỉ chấp nhận http:// hoặc https://.";
  }

  const host = url.hostname.toLowerCase();
  if (host === "localhost" || host === "[::1]" || isPrivateIPv4(host)) {
    return BLOCKED_MESSAGE;
  }
  return null;
}