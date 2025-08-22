using Amazon.Lambda.ApplicationLoadBalancerEvents;
using HttpMetrics.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HttpMetrics
{
    public static class ClientHttpMetrics
    {

        private static IServiceProvider? _provider;
        private static readonly object _lock = new();
        private static int _started;
        private static TracerProvider? _tracerProvider;


        public static void Start(MetricOption? option = null)
        {
            if (Interlocked.Exchange(ref _started, 1) == 1) return;


            var sp = BuildInternalProvider(option);
            var opt = sp.GetRequiredService<MetricOption>();
            var buffer = sp.GetRequiredService<IMetricBuffer>();


            _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddHttpClientInstrumentation(o =>
            {
                o.RecordException = true;


                o.FilterHttpRequestMessage = req =>
                {
                    var uri = req.RequestUri;
                    if (uri is null) return false;


                    if (opt.AllowedHosts?.Length > 0 &&
        !Array.Exists(opt.AllowedHosts, h => string.Equals(h, uri.Host, StringComparison.OrdinalIgnoreCase)))
                        return false;


                    return RouteMatcher.TryMatch(opt.Routes, uri) is not null;
                };


                o.EnrichWithHttpRequestMessage = (activity, req) =>
                {
                    var uri = req.RequestUri;
                    if (uri is null) return;


                    activity.SetTag("url.full", uri.ToString());
                    activity.SetTag("http.request.method", req.Method.Method);


                    var match = RouteMatcher.TryMatch(opt.Routes, uri);
                    if (match is not null && !string.IsNullOrWhiteSpace(match.Name))
                        activity.SetTag("api.name", match.Name);
                };


                o.EnrichWithHttpResponseMessage = (activity, resp) =>
                activity.SetTag("http.response.status_code", (int)resp.StatusCode);


                o.EnrichWithException = (activity, ex) =>
                {
                    activity.SetTag("exception.type", ex.GetType().FullName);
                    activity.SetTag("exception.message", ex.Message);
                    activity.SetTag("exception.stacktrace", ex.StackTrace);
                };
            })
            .AddProcessor(new ApiLogProcessor(opt, buffer))
            .Build();


            AppDomain.CurrentDomain.ProcessExit += (_, __) => { try { _tracerProvider?.Dispose(); } catch { } };
        }

        public static void StartFromEnvironment() => Start(MetricOptionLoader.FromEnvironment());

        /// <summary>
        /// Appends a "clientMetrics" property to the existing JSON response body.
        /// If the body is not valid JSON, it is returned unchanged.
        /// </summary>
        public static ApplicationLoadBalancerResponse WrapResponse(ApplicationLoadBalancerResponse response)
        {
            if (response is null || string.IsNullOrEmpty(response.Body)) return response;
            if (JsonNode.Parse(response.Body) is not JsonObject node) return response;


            var buffer = BuildInternalProvider(null).GetRequiredService<IMetricBuffer>();
            var snapshot = buffer.SnapshotAndClear();


            node["clientMetrics"] = JsonSerializer.SerializeToNode(snapshot.Metrics);
            node["clientMetricsInfo"] = new JsonObject
            {
                ["totalCaptured"] = snapshot.TotalCaptured,
                ["returned"] = snapshot.Metrics.Count,
                ["dropped"] = snapshot.Dropped
            };


            response.Body = node.ToJsonString();
            return response;
        }

        private static IServiceProvider BuildInternalProvider(MetricOption? opt)
        {
            lock (_lock)
            {
                if (_provider != null) return _provider;


                var services = new ServiceCollection();
                services.AddSingleton(opt ?? MetricOptionLoader.FromEnvironment());
                services.AddSingleton<IMetricBuffer, MetricBuffer>();


                _provider = services.BuildServiceProvider();
                return _provider;
            }
        }
    }
}
