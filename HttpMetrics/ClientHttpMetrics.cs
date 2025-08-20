using Amazon.Lambda.ApplicationLoadBalancerEvents;
using OpenTelemetry.Trace;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HttpMetrics
{
    public static class ClientHttpMetrics
    {
        private static TracerProvider? tracerProvider;
        private static int _started;

        public static void Start(MetricOption metricOption)
        {
            if (Interlocked.Exchange(ref _started, 1) == 1)
                return;

            if (metricOption == null)
                return;

            tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                .AddHttpClientInstrumentation(o =>
                {
                    o.RecordException = true;
                    o.FilterHttpRequestMessage = req =>
                    {
                        var uri = req.RequestUri;
                        if (uri == null)
                            return false;

                        if (metricOption.AllowedHosts?.Length > 0 &&
                            !Array.Exists(metricOption.AllowedHosts, h =>
                                string.Equals(h, uri.Host, StringComparison.OrdinalIgnoreCase)))
                            return false;

                        var match = RouteMatcher.TryMatch(metricOption.Routes, uri);
                        if (match == null)
                            return false;

                        System.Diagnostics.Activity.Current?.SetTag("api.name", match.Name);
                        return true;
                    };
                })
                .AddProcessor(new ApiLogProcessor(metricOption))
                .Build();

            AppDomain.CurrentDomain.ProcessExit += (_, __) => tracerProvider.Dispose();
        }

        public static void StartFromEnvironment() =>
            Start(MetricOptionLoader.FromEnvironment());

        public static ApplicationLoadBalancerResponse WrapResponse(ApplicationLoadBalancerResponse response)
        {
            if (string.IsNullOrEmpty(response.Body))
                return response;

            var node = JsonNode.Parse(response.Body) as JsonObject;
            if (node == null)
                return response;

            var metrics = MetricSnapshotContext.SnapshotAndClear();
            node["clientMetrics"] = JsonSerializer.SerializeToNode(metrics);
            response.Body = node.ToJsonString();

            return response;
        }

        private static object TryParseJson(string body)
        {
            try
            {
                return JsonSerializer.Deserialize<object>(body);
            }
            catch
            {
                return body;
            }
        }
    }
}
