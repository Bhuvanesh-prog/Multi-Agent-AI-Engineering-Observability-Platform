using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Orbit.ControlPlane.Api.Tests;

public sealed class HealthAndMetaTests(OrbitApiFactory factory) : IClassFixture<OrbitApiFactory>
{
    private sealed record Meta(string Service, string Version, string Environment);

    [Fact]
    public async Task Live_ProcessUp_Returns200()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_DatabaseReachable_Returns200()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMeta_Always_ReturnsServiceAndEnvironment()
    {
        using var client = factory.CreateClient();

        var meta = await client.GetFromJsonAsync<Meta>("/api/v1/meta", TestContext.Current.CancellationToken);

        meta.ShouldNotBeNull();
        meta.Service.ShouldBe("orbit-control-plane");
        meta.Environment.ShouldBe("Testing");
    }

    [Fact]
    public async Task UnknownRoute_Always_ReturnsProblemDetails()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}

public sealed class StartupFailureTests
{
    [Fact]
    public async Task Ready_DatabaseUnreachable_Returns503()
    {
        await using var factory = new UnreachableDatabaseFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Startup_MissingConnectionString_Throws()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing").UseSetting("ConnectionStrings:Orbit", ""));

        var exception = Should.Throw<InvalidOperationException>(() => factory.CreateClient());

        exception.Message.ShouldContain("ConnectionStrings:Orbit");
    }
}
