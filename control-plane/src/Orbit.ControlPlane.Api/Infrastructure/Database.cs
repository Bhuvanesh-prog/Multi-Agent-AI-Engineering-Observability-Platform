namespace Orbit.ControlPlane.Api.Infrastructure;

internal static class Database
{
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Registers the Orbit Postgres data source and its readiness check. Fails fast when the
        /// connection string is missing: it comes from user-secrets / env locally and Key Vault in Azure.
        /// </summary>
        public void AddOrbitDatabase()
        {
            var connectionString = builder.Configuration.GetConnectionString("Orbit");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:Orbit is not configured. Set it with user-secrets, the ConnectionStrings__Orbit environment variable, or Key Vault.");
            }

            builder.Services.AddNpgsqlDataSource(connectionString);
            builder.Services.AddHealthChecks().AddNpgSql(name: "postgres", tags: [HealthTags.Ready]);
        }
    }
}
