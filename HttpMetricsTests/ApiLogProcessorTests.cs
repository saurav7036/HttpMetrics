using HttpMetrics;
using HttpMetrics.Interfaces;
using HttpMetrics.Models;
using Moq;
using OpenTelemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace HttpMetricsTests
{
    

    public class ApiLogProcessorTests
    {
        private static Activity CreateTestActivity(Uri url, string method = "GET", int statusCode = 200)
        {
            var activity = new Activity("HttpClient");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();

            activity.SetTag("url.full", url.ToString());
            activity.SetTag("http.request.method", method);
            activity.SetTag("http.response.status_code", statusCode);

            return activity;
        }

        [Fact]
        public void OnEnd_ValidSpan_AddsToBuffer()
        {
            // Arrange
            var url = new Uri("https://testhost.com/api/values/123");
            var activity = CreateTestActivity(url);

            var bufferMock = new Mock<IMetricBuffer>();
            var opt = new MetricOption
            {
                AllowedHosts = new[] { "testhost.com" },
                Routes = new[] { new Route { Template = "/api/values/{id}", Name = "testApi" } }
            };

            var processor = new ApiLogProcessor(opt, bufferMock.Object);

            // Act
            processor.OnEnd(activity);

            // Assert
            bufferMock.Verify(b => b.Add(It.Is<PerformanceMetricData>(
                m => m.ApiName == "testApi" && m.HttpMethod == "GET"
            )), Times.Once);
        }

        [Fact]
        public void OnEnd_HostNotAllowed_SkipsBuffering()
        {
            var url = new Uri("https://otherhost.com/api/values/123");
            var activity = CreateTestActivity(url);

            var bufferMock = new Mock<IMetricBuffer>();
            var opt = new MetricOption
            {
                AllowedHosts = new[] { "testhost.com" },
                Routes = new[] { new Route { Template="/api/values/{id}", Name="testApi" } }
            };

            var processor = new ApiLogProcessor(opt, bufferMock.Object);

            processor.OnEnd(activity);

            bufferMock.Verify(b => b.Add(It.IsAny<PerformanceMetricData>()), Times.Never);
        }

        [Fact]
        public void OnEnd_NoRouteMatch_SkipsBuffering()
        {
            var url = new Uri("https://testhost.com/api/other");
            var activity = CreateTestActivity(url);

            var bufferMock = new Mock<IMetricBuffer>();
            var opt = new MetricOption
            {
                AllowedHosts = new[] { "testhost.com" },
                Routes = new[] { new Route { Template = "/api/values/{id}", Name = "testApi" } }
            };

            var processor = new ApiLogProcessor(opt, bufferMock.Object);

            processor.OnEnd(activity);

            bufferMock.Verify(b => b.Add(It.IsAny<PerformanceMetricData>()), Times.Never);
        }

        [Fact]
        public void OnEnd_HandlesStringStatusCode()
        {
            var url = new Uri("https://testhost.com/api/values/123");
            var activity = CreateTestActivity(url);
            activity.SetTag("http.response.status_code", "200");

            var bufferMock = new Mock<IMetricBuffer>();
            var opt = new MetricOption
            {
                AllowedHosts = new[] { "testhost.com" },
                Routes = new[] { new Route { Template = "/api/values/{id}", Name = "testApi" } }
            };

            var processor = new ApiLogProcessor(opt, bufferMock.Object);

            processor.OnEnd(activity);

            bufferMock.Verify(b => b.Add(It.IsAny<PerformanceMetricData>()), Times.Once);
        }
    }

}
