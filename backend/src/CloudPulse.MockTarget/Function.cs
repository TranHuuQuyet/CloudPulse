
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Collections.Generic;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace CloudPulse.MockTarget;

public class Function
{
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request)
    {
        // Doc cac tham so tren URL
        var query = request.QueryStringParameters;

        // Mac dinh tra ve HTTP 200
        int statusCode = 200;

        // Neu status=500 thi gia lap loi server
        if (query != null &&
            query.TryGetValue("status", out string? status) &&
            status == "500")
        {
            statusCode = 500;
        }

        // Neu co delay hop le, cho truoc khi phan hoi
        if (query != null &&
            query.TryGetValue("delay", out string? delay) &&
            int.TryParse(delay, out int delayMs) &&
            delayMs >= 0 && delayMs <= 10000)
        {
            await Task.Delay(delayMs);
        }

        // Tra ve HTTP Response
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = statusCode,

            Headers = new Dictionary<string, string>
            {
                ["content-type"] = "application/json"
            },

            Body = JsonSerializer.Serialize(new
            {
                simulatedStatus = statusCode
            })
        };
    }
}
