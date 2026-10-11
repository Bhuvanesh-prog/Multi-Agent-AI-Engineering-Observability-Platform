using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Orbit.ControlPlane.Api.Infrastructure;

namespace Orbit.ControlPlane.Api.Features.Meta;

/// <summary>Contract for 🎨 Pixel: GET /api/v1/meta → 200 MetaResponse.</summary>
internal sealed record MetaResponse(string Service, string Version, string Environment);

internal static class MetaEndpoints
{
    private static readonly string Version =
        typeof(MetaEndpoints).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

    extension(IEndpointRouteBuilder api)
    {
        public IEndpointRouteBuilder MapMetaEndpoints()
        {
            api.MapGet("/meta", GetMeta)
                .WithName("GetMeta")
                .WithSummary("Service name, version and environment")
                .WithTags("Meta");
            return api;
        }
    }

    internal static Ok<MetaResponse> GetMeta(IHostEnvironment environment) =>
        TypedResults.Ok(new MetaResponse(Telemetry.ServiceName, Version, environment.EnvironmentName));
}
