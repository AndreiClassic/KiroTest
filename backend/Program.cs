using backend.DataAccess.Configuration;
using backend.DataAccess.Repositories;
using backend.Services;
using Serilog;
using Serilog.Sinks.Elasticsearch;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "InsuranceCalculator")
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "insurance-logs-{0:yyyy.MM.dd}",
        NumberOfShards = 1,
        NumberOfReplicas = 0
    })
    .CreateLogger();

try
{
    Log.Information("Starting Insurance Calculator API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Configure MongoDB settings
    builder.Services.Configure<MongoDbSettings>(
        builder.Configuration.GetSection("MongoDb"));

    // Register repositories
    builder.Services.AddSingleton<IInsuranceQuoteRepository, InsuranceQuoteRepository>();

    // Register HttpClient and LotService
    builder.Services.AddHttpClient<ILotService, LotService>();

    builder.Services.AddControllers();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowVue", policy =>
        {
            policy.WithOrigins("http://localhost:8080", "http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    // Add Serilog request logging
    app.UseSerilogRequestLogging();

    app.UseCors("AllowVue");
    app.MapControllers();

    Log.Information("Insurance Calculator API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
