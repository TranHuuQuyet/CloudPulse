using System.Net;
using System.Net.Sockets;

namespace CloudPulse.Core.Validation;

public static class UrlValidator
{
    private const string PrivateOrLoopbackErrorMessage 
    = "Private or loopback IP addresses are not permitted."; 

    // Kiểm tra toàn diện URL bao gồm cả phân giải DNS để chống tấn công SSRF & DNS Rebinding

    public static async Task<UrlValidationResult> ValidateAsync(string? url, CancellationToken cancellationToken = default)
    {
        // Kiểm tra URL null hoặc rỗng
        if (string.IsNullOrWhiteSpace(url))
        {
            return UrlValidationResult.Invalid("URL is null or empty.", null);
        }

        // Parse cấu trục Uri
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return UrlValidationResult.Invalid("Invalid URL format.", null);
        }

        //Chỉ cho phép các scheme http và https
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return UrlValidationResult.Invalid("Only HTTP and HTTPS schemes are allowed.", null);
        }

        // Kiểm tra nhanh hostname 'localhost'
        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) || uri.IsLoopback)
        {
            return UrlValidationResult.Invalid(PrivateOrLoopbackErrorMessage, uri.Host);
        }

        // Nếu Host nhập vào IP tĩnh trực tiếp (EX: http://192.168.1.1)
        if (IPAddress.TryParse(uri.Host, out var ipAddress))
        {
            if (IsRestrictedIp(ipAddress))
            {
                return UrlValidationResult.Invalid(PrivateOrLoopbackErrorMessage, uri.Host);
            }
            return UrlValidationResult.Valid(uri);
        }

        // Phân giải DNS nếu Host là domain (Chống DNS Rebinding như 127.0.0.1.nip.io)
        try
        {
            var hostAddresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
            if (hostAddresses.Length == 0)
            {
                return UrlValidationResult.Invalid("Unable to resolve host domain.", uri.Host, isDnsError: true);
            }
            foreach (var ip in hostAddresses)
            {
                if (IsRestrictedIp(ip))
                {
                    return UrlValidationResult.Invalid(PrivateOrLoopbackErrorMessage, ip.ToString());
                }
            }
        }
        
        catch (SocketException)
        {
            return UrlValidationResult.Invalid("DNS resolution failed or host not found.", uri.Host, isDnsError: true);
        }
        catch (Exception ex)
        {
            return UrlValidationResult.Invalid($"Validation error: {ex.Message}", uri.Host);
        }
        // Tất cả các bài kiểm tra đều vượt qua an toàn
        return UrlValidationResult.Valid(uri);
    }
    /// <summary>
    /// Kiểm tra một địa chỉ IP có nằm trong danh sách cấm (Loopback, Private RFC 1918, AWS Metadata) không.
    /// </summary>
    public static bool IsRestrictedIp(IPAddress ip)
    {
        // Chuẩn hóa nếu là IPv4-mapped IPv6
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }
        // 1. Chặn Loopback (127.0.0.0/8 và ::1)
        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }
        // 2. Chặn IPv6 nội bộ / link-local / multicast
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6Multicast || ip.IsIPv6SiteLocal)
            {
                return true;
            }
            // Chặn IPv6 Unique Local Address (fc00::/7)
            var bytesV6 = ip.GetAddressBytes();
            if ((bytesV6[0] & 0xFE) == 0xFC)
            {
                return true;
            }
            return false;
        }
        // 3. Kiểm tra các dải IPv4
        var bytes = ip.GetAddressBytes();
        // 0.0.0.0/8 (Mạng hiện tại)
        if (bytes[0] == 0) return true;
        // 10.0.0.0/8 (RFC 1918 Private Class A)
        if (bytes[0] == 10) return true;
        // 100.64.0.0/10 (Carrier-Grade NAT)
        if (bytes[0] == 100 && (bytes[1] >= 64 && bytes[1] <= 127)) return true;
        // 127.0.0.0/8 (Loopback IPv4)
        if (bytes[0] == 127) return true;
        // 169.254.0.0/16 (Link-Local & AWS Metadata 169.254.169.254)
        if (bytes[0] == 169 && bytes[1] == 254) return true;
        // 172.16.0.0/12 (RFC 1918 Private Class B: 172.16.0.0 - 172.31.255.255)
        if (bytes[0] == 172 && (bytes[1] >= 16 && bytes[1] <= 31)) return true;
        // 192.168.0.0/16 (RFC 1918 Private Class C: 192.168.0.0 - 192.168.255.255)
        if (bytes[0] == 192 && bytes[1] == 168) return true;
        // 255.255.255.255 (Broadcast)
        if (bytes[0] == 255 && bytes[1] == 255 && bytes[2] == 255 && bytes[3] == 255) return true;
        return false;
    }
}