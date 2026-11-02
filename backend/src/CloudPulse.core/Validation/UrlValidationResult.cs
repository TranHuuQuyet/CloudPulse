namespace CloudPulse.Core.Validation;

public class UrlValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    public Uri? ValidatedUri { get; }
    public string? BlockedIpOrHost { get; }

    private UrlValidationResult(bool isValid, string? errorMessage, Uri? validatedUri, string? blockedIpHost)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        ValidatedUri = validatedUri;
        BlockedIpOrHost = blockedIpHost;
    }

    public static UrlValidationResult Valid(Uri validatedUri)
    {
        return new UrlValidationResult(true, null, validatedUri, null);
    }
    public static UrlValidationResult Invalid(string errorMessage, string? blockedIpHost)
    {
        return new UrlValidationResult(false, errorMessage, null, blockedIpHost);
    }

}
