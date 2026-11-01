
using Amazon.Lambda.APIGatewayEvents;
using CloudPulse.MockTarget;
using System.Diagnostics;

var function = new Function();

await TestAsync("Default 200", new Dictionary<string, string>(), 200);
await TestAsync("Error 500", new Dictionary<string, string>
{
    ["status"] = "500"
}, 500);
await TestAsync("Delay 7000", new Dictionary<string, string>
{
    ["delay"] = "7000"
}, 200);

async Task TestAsync(
    string name,
    Dictionary<string, string> query,
    int expectedStatus)
{
    var request = new APIGatewayHttpApiV2ProxyRequest
    {
        RawPath = "/mock",
        QueryStringParameters = query
    };

    var timer = Stopwatch.StartNew();
    var response = await function.FunctionHandler(request);
    timer.Stop();

    Console.WriteLine(
        $"{name}: HTTP {response.StatusCode}, " +
        $"{timer.ElapsedMilliseconds} ms, {response.Body}");

    if (response.StatusCode != expectedStatus)
    {
        throw new Exception($"{name} failed!");
    }

    if (name == "Delay 7000" &&
        timer.ElapsedMilliseconds < 6900)
    {
        throw new Exception("Delay is too short!");
    }
}
