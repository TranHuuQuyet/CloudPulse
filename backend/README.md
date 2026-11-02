
# CloudPulse Backend - Week 1

## Requirements

- .NET 8 SDK

## W1-06: CloudPulse.MockTarget

Build:

```powershell
dotnet build backend/src/CloudPulse.MockTarget/CloudPulse.MockTarget.csproj
```

Run smoke tests:

```powershell
dotnet run --project backend/tests/CloudPulse.MockTarget.SmokeTests
```

Expected scenarios:

- GET /mock -> HTTP 200
- GET /mock?status=500 -> HTTP 500
- GET /mock?delay=7000 -> response after about 7 seconds

Note: Smoke tests invoke the Lambda handler locally.
HTTP API Gateway deployment is not yet verified.

## W1-07: CloudPulse.Api

Build:

```powershell
dotnet build backend/src/CloudPulse.Api/CloudPulse.Api.csproj
```

Run locally:

```powershell
dotnet run --project backend/src/CloudPulse.Api/CloudPulse.Api.csproj -- --urls http://localhost:5080
```

Test endpoint:

GET http://localhost:5080/api/status

Expected:

HTTP 200 with JSON status "running".

## AWS Integration

AWS deployment and API Gateway integration
will be coordinated with the infrastructure team.
