using Xunit;

public class EnvFixture
{
    static EnvFixture()
    {
        Environment.SetEnvironmentVariable("METRICS_MAX_ITEMS", "3");
        Environment.SetEnvironmentVariable("METRICS_DRAIN_LIMIT", "2");
    }
}

[CollectionDefinition("Env collection")]
public class EnvCollection : ICollectionFixture<EnvFixture> { }
