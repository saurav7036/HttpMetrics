namespace HttpMetrics;

public interface IApiLogWriter
{
    Task WriteLogAsync(ApiLog log);
}
