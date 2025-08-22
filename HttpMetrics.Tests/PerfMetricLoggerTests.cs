using System.Diagnostics;
using HttpMetrics;
using Moq;
using Xunit;

[Collection("Env collection")]
public class PerfMetricLoggerTests
{
    [Fact]
    public void OnEnd_WritesApiLog_ForSuccessfulSpan()
    {
        var opt = new MetricOption
        {
            AllowedHosts = new[] { "example.com" },
            Routes = new[] { new Route { Template = "/api/{id}", Name = "Test" } }
        };

        var processor = new ApiLogProcessor(opt);

        var mockWriter = new Moq.Mock<IApiLogWriter>();
        ApiLog? captured = null;
        mockWriter.Setup(w => w.WriteLogAsync(It.IsAny<ApiLog>()))
            .Callback<ApiLog>(log => captured = log)
            .Returns(Task.CompletedTask);

        PerfMetricLogger.LogWriter = mockWriter.Object;

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> opts) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource("test");
        using var activity = source.StartActivity("test", ActivityKind.Client)!;
        activity.SetTag("url.full", "https://example.com/api/123");
        activity.SetTag("http.request.method", "GET");
        activity.SetTag("http.response.status_code", 200);
        activity.SetEndTime(activity.StartTimeUtc.AddMilliseconds(50));
        activity.Stop();

        processor.OnEnd(activity);

        mockWriter.Verify(w => w.WriteLogAsync(It.IsAny<ApiLog>()), Times.Once);
        Assert.NotNull(captured);
        Assert.Equal("chatops_perf", captured!.ApplicationName);
        Assert.Equal("GET", captured.Verb);
        Assert.Equal("Test", captured.Api);
        Assert.Equal("https://example.com/api/123", captured.Url);
        Assert.True(captured.IsSuccessful);

        PerfMetricLogger.LogWriter = new FileApiLogWriter();
        new DefaultMetricSnapshotContext().SnapshotAndClear(out _, out _);
    }
}
