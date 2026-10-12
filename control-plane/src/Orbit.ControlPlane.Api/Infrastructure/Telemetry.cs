using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Orbit.ControlPlane.Api.Infrastructure;

internal static class Telemetry
{
    public const string ServiceName = "orbit-control-plane";

    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Traces, metrics and logs via OpenTelemetry. Exports OTLP to the collector only when
        /// OTEL_EXPORTER_OTLP_ENDPOINT is set, so local runs and tests don't spam failed exports.
        /// </summary>
        public void AddOrbitTelemetry()
        {
            var otel = builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(ServiceName))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context => !context.Request.Path.StartsWithSegments("/health", StringComparison.Ordinal))
                    .AddHttpClientInstrumentation())
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation())
                .WithLogging();

            if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            {
                otel.UseOtlpExporter();
            }
        }
    }
}
