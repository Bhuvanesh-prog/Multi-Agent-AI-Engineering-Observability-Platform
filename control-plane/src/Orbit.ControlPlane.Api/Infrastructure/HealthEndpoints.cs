using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Orbit.ControlPlane.Api.Infrastructure;

internal static class HealthTags
{
    public const string Ready = "ready";
}

internal static class HealthEndpoints
{
    extension(WebApplication app)
    {
        /// <summary>/health/live: process is up (no dependencies). /health/ready: dependencies reachable.</summary>
        public void MapOrbitHealthChecks()
        {
            app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
            app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(HealthTags.Ready) });
        }
    }
}
