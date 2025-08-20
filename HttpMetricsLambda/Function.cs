using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.ApplicationLoadBalancerEvents;
using HttpMetrics;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HttpMetricsLambda;

public class Function
{
    private static readonly HttpClient _httpClient = new();

    static Function()
    {
        // Initialize HttpMetrics using environment configuration
        ClientHttpMetrics.StartFromEnvironment();
    }

    /// <summary>
    /// Sample Lambda handler which performs an HTTP call and wraps the response
    /// with any collected client metrics.
    /// </summary>
    public async Task<ApplicationLoadBalancerResponse> FunctionHandler(ApplicationLoadBalancerRequest request, ILambdaContext context)
    {
        // Perform a simple HTTP GET to generate metrics
        await _httpClient.GetAsync("https://example.com");

        var response = new ApplicationLoadBalancerResponse
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = "{\"message\":\"ok\"}"
        };

        // Wrap the response to include metric snapshot
        return ClientHttpMetrics.WrapResponse(response);
    }
}
