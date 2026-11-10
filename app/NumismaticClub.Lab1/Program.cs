using OpenTelemetry.Metrics;
using OpenTelemetry.Instrumentation.Process;

var builder = WebApplication.CreateBuilder(args);

// Добавляем метрики Prometheus
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Включаем эндпоинт /metrics для Prometheus
app.UseOpenTelemetryPrometheusScrapingEndpoint();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();