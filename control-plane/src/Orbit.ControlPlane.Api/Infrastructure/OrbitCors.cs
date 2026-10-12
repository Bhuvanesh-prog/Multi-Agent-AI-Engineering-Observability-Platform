namespace Orbit.ControlPlane.Api.Infrastructure;

internal static class OrbitCors
{
    public const string PolicyName = "orbit-ui";

    extension(WebApplicationBuilder builder)
    {
        /// <summary>Explicit origin allow-list from Cors:AllowedOrigins; empty means no cross-origin access.</summary>
        public void AddOrbitCors()
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            builder.Services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
                .WithOrigins(origins)
                .WithMethods("GET", "POST", "PUT", "DELETE")
                .AllowAnyHeader()));
        }
    }
}
