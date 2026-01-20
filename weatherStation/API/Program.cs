using API.Extensions;
using API.Interface;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddApplicationServices(builder.Configuration);

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/hello", static () =>
{
    HelloResponse response = new HelloResponse("Hello from WeatherStation API");
    return Results.Ok(response);
})
.WithName("GetHello");

app.MapGet("/health", static () =>
{
    HealthResponse response = new HealthResponse("Healthy");
    return Results.Ok(response);
})
.WithName("GetHealth");

app.MapPost("/api/ingest", static async (
    IWeatherIngestService ingestService,
    CancellationToken cancellationToken
) =>
{
    int writtenCount = await ingestService.FetchAndStoreAsync(cancellationToken);
    IngestResponse response = new IngestResponse(writtenCount);
    return Results.Ok(response);
})
.WithName("IngestWeatherReadings");

app.Run();

record HelloResponse(string Message);

record HealthResponse(string Status);

record IngestResponse(int WrittenCount);
