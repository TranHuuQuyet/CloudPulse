
param(
    [string]$BaseUrl = "http://localhost:5080"
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd("/")
$monitorId = $null
$passed = 0

function Assert-Ok {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw "FAILED: $Message"
    }

    $script:passed++
    Write-Host "PASS: $Message" -ForegroundColor Green
}

function Get-ErrorStatus {
    param($ErrorRecord)

    $response = $ErrorRecord.Exception.Response
    if ($null -ne $response) {
        return [int]$response.StatusCode
    }

    return 0
}
try {
    # 1. API status
    $status = Invoke-RestMethod "$BaseUrl/api/status"
    Assert-Ok ($status.status -eq "running") "API Status"

    # 2. Local Development CORS
    $headers = @{
        Origin = "http://localhost:5173"
        "Access-Control-Request-Method" = "POST"
        "Access-Control-Request-Headers" = "content-type"
    }

    $preflight = Invoke-WebRequest `
        -Uri "$BaseUrl/api/monitors" `
        -Method Options `
        -Headers $headers `
        -UseBasicParsing

    $allowedOrigin = [string]$preflight.Headers[
        "Access-Control-Allow-Origin"
    ]

    Assert-Ok (
        $allowedOrigin -eq "http://localhost:5173"
    ) "CORS Preflight"

    # 3. SSRF protection
    foreach ($blockedUrl in @(
        "http://127.0.0.1",
        "http://169.254.169.254/latest/meta-data/"
    )) {
        $body = @{
            name = "SSRF Test"
            url = $blockedUrl
        } | ConvertTo-Json

        $errorCode = 0

        try {
            $null = Invoke-RestMethod `
                -Uri "$BaseUrl/api/monitors" `
                -Method Post `
                -ContentType "application/json" `
                -Body $body
        }
        catch {
            $errorCode = Get-ErrorStatus $_
        }

        Assert-Ok ($errorCode -eq 400) "SSRF: $blockedUrl"
    }

    # 4. Create Monitor
    $body = @{
        name = "TV3 W3 Smoke Test"
        url = "https://example.com"
    } | ConvertTo-Json

    $created = Invoke-RestMethod `
        -Uri "$BaseUrl/api/monitors" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body

    $monitorId = $created.monitorId

    Assert-Ok (
        -not [string]::IsNullOrWhiteSpace($monitorId)
    ) "Create Monitor"

    # 5. Get Monitor detail
    $detail = Invoke-RestMethod `
        "$BaseUrl/api/monitors/$monitorId"

    Assert-Ok (
        $detail.monitorId -eq $monitorId
    ) "Get Monitor Detail"

    # 6. List Monitors
    $monitors = @(Invoke-RestMethod "$BaseUrl/api/monitors")

    Assert-Ok (
        @($monitors | Where-Object {
            $_.monitorId -eq $monitorId
        }).Count -eq 1
    ) "List Monitors"

    # 7. Check Now twice
    for ($i = 1; $i -le 2; $i++) {
        $check = Invoke-RestMethod `
            -Uri "$BaseUrl/api/monitors/$monitorId/check" `
            -Method Post

        $keys = @($check.PSObject.Properties.Name)

        $contractOk = @(
            "monitorId", "checkedAt", "isSuccess",
            "statusCode", "responseTimeMs",
            "errorType", "errorMessage"
        ) | Where-Object { $_ -notin $keys }

        Assert-Ok (
            $check.monitorId -eq $monitorId -and
            @($contractOk).Count -eq 0
        ) "Check Now #$i"
    }
    # 8. Check History with limit
    $checksResponse = Invoke-WebRequest `
        -Uri "$BaseUrl/api/monitors/$monitorId/checks?limit=1" `
        -UseBasicParsing

    Assert-Ok (
        [int]$checksResponse.StatusCode -eq 200
    ) "Checks HTTP 200"

    # Parse JSON directly, avoiding pipeline enumeration
    $parsedChecks = ConvertFrom-Json `
        -InputObject $checksResponse.Content

    # Support both JSON array and wrapped { value: [] }
    if ($null -ne $parsedChecks.PSObject.Properties["value"]) {
        $checks = @($parsedChecks.value)
    }
    else {
        $checks = @($parsedChecks)
    }

    Assert-Ok (
        $checks.Count -eq 1
    ) "Checks limit=1"

    $chartFields = @(
        $checks[0].PSObject.Properties |
        ForEach-Object { $_.Name }
    )

    $requiredFields = @(
        "checkedAt",
        "time",
        "responseTimeMs",
        "statusCode"
    )

    $missingFields = @(
        $requiredFields | Where-Object {
            $_ -notin $chartFields
        }
    )

    if ($missingFields.Count -gt 0) {
        Write-Host "Missing fields: $($missingFields -join ', ')" `
            -ForegroundColor Red
    }

    Assert-Ok (
        $missingFields.Count -eq 0
    ) "Checks JSON Contract"


    # 9. Incident History
    $incidents = @(
        Invoke-RestMethod `
            "$BaseUrl/api/monitors/$monitorId/incidents?limit=10"
    )

    Assert-Ok (
        $incidents.Count -le 10
    ) "Incidents limit=10"

    Write-Host ""
    Write-Host "ALL $passed TESTS PASSED" -ForegroundColor Green
}
finally {
    # Cleanup: remove test monitor
    if (-not [string]::IsNullOrWhiteSpace($monitorId)) {
        try {
            Invoke-RestMethod `
                -Uri "$BaseUrl/api/monitors/$monitorId" `
                -Method Delete

            Write-Host "Test monitor deleted."
        }
        catch {
            Write-Warning "Cleanup failed for monitor $monitorId"
        }
    }
}
