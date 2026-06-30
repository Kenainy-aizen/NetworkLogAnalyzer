using Microsoft.EntityFrameworkCore;
using Storage;
using Storage.Repositories;
using Parser;
using Collector;

var builder = WebApplication.CreateBuilder(args);

// CORS : autoriser React (port 5173)
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Base de données SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=logs.db"));

// Injection de dépendances
builder.Services.AddScoped<IEventRepository, EventRepository>();

// Parsers (le Collector les utilisera tous)
builder.Services.AddScoped<ILogParser, JournalParser>();
builder.Services.AddScoped<ILogParser, IptablesParser>();

// Collector en arrière-plan
builder.Services.AddHostedService<CollectorBackgroundService>();

// SignalR (temps réel)
builder.Services.AddSignalR();

// API REST + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Créer la base de données automatiquement au démarrage
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors("ReactApp");
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHub<Api.Hubs.LogHub>("/hubs/logs");

app.Run();
