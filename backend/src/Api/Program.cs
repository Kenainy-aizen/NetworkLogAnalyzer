using Microsoft.EntityFrameworkCore;
using Storage;
using Storage.Repositories;
using Parser;
using Collector;
using Analyzer;
using Analyzer.Rules;
using Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var dbProvider = builder.Configuration["DatabaseProvider"] ?? "PostgreSQL";
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider == "SQLite")
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection"));
    else
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<INotifier, SignalRNotifier>();
builder.Services.AddScoped<IAnalysisTrigger, AnalysisTrigger>();

builder.Services.AddScoped<ILogParser, JournalParser>();
builder.Services.AddScoped<ILogParser, IptablesParser>();
builder.Services.AddScoped<ILogParser, PamParser>();
builder.Services.AddScoped<ILogParser, FirewalldParser>();

builder.Services.AddScoped<IDetectionRule, SshBruteForceRule>();
builder.Services.AddScoped<IDetectionRule, PortScanRule>();
builder.Services.AddScoped<IDetectionRule, FtpBruteForceRule>();
builder.Services.AddScoped<IDetectionRule, HttpBruteForceRule>();
builder.Services.AddScoped<IDetectionRule, HttpFloodRule>();
builder.Services.AddScoped<AnalyzerService>();

builder.Services.AddHostedService<CollectorBackgroundService>();
builder.Services.AddHostedService<NginxLogCollector>();
builder.Services.AddHostedService<ApacheLogCollector>();
builder.Services.AddHostedService<VsftpdLogCollector>();
builder.Services.AddHostedService<Fail2banLogCollector>();

builder.Services.AddSignalR();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ne pas appeler EnsureCreated en mode test (InMemory + SQLite ne peuvent coexister)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors("ReactApp");
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHub<Api.Hubs.LogHub>("/hubs/logs");

app.Run();

public partial class Program { }
