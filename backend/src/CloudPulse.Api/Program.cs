using Amazon.Lambda.AspNetCoreServer.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

app.MapGet("/api/status", () =>
{
    return Results.Ok(new
    {
        service = "CloudPulse.Api",
        status = "running",
        message = "CloudPulse API is working!"
    });
});

app.Run();