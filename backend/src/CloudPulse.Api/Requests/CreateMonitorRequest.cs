namespace CloudPulse.Api.Requests;

public sealed record CreateMonitorRequest(
    string? Name,
    string? Url
);