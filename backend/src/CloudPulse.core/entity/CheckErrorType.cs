namespace CloudPulse.Core.Entity;

public static class CheckErrorType
{
    public const string None = "None";
    public const string HttpError = "HttpError";
    public const string Timeout = "Timeout";
    public const string ConnectionError = "ConnectionError";
    public const string DnsError = "DnsError";
    public const string SsrfBlocked = "SSRFBlocked";
    public const string Unknown = "Unknown";
}

