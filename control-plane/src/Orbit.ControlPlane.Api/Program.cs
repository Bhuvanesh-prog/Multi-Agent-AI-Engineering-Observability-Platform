using Orbit.ControlPlane.Api.Features.Meta;
using Orbit.ControlPlane.Api.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.AddOrbitTelemetry();
builder.AddOrbitDatabase();
builder.AddOrbitCors();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors(OrbitCors.PolicyName);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapOrbitHealthChecks();

var api = app.MapGroup("/api/v1");
api.MapMetaEndpoints();

await app.RunAsync();
