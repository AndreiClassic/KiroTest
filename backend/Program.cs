using backend.DataAccess.Configuration;
using backend.DataAccess.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

// Register repositories
builder.Services.AddSingleton<IInsuranceQuoteRepository, InsuranceQuoteRepository>();

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

app.UseCors("AllowVue");
app.MapControllers();

app.Run();
