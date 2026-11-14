namespace CloudPulse.Core.Validation;

public class UrlValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    public Uri? ValidatedUri { get; }
    public string? BlockedIpOrHost { get; }
    public bool IsDnsError { get; }

    private UrlValidationResult(bool isValid, string? errorMessage, Uri? validatedUri, string? blockedIpHost, bool isDnsError = false)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        ValidatedUri = validatedUri;
        BlockedIpOrHost = blockedIpHost;
        IsDnsError = isDnsError;
    }

    public static UrlValidationResult Valid(Uri validatedUri)
    {
        return new UrlValidationResult(true, null, validatedUri, null, false);
    }
    public static UrlValidationResult Invalid(string errorMessage, string? blockedIpHost, bool isDnsError = false)
    {
        return new UrlValidationResult(false, errorMessage, null, blockedIpHost, isDnsError);
    }

}
