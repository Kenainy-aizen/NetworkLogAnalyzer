using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Storage;

namespace Tests.Integration;

public class WebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Supprimer TOUT ce qui touche à AppDbContext et SQLite
            var toRemove = services
                .Where(d =>
                    d.ServiceType.FullName != null && (
                    d.ServiceType.FullName.Contains("DbContext") ||
                    d.ServiceType.FullName.Contains("EntityFramework") ||
                    d.ImplementationType?.FullName?.Contains("Sqlite") == true ||
                    d.ImplementationType?.FullName?.Contains("DbContext") == true))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            // Ajouter InMemory proprement
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Désactiver les BackgroundServices
            var hosted = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var d in hosted)
                services.Remove(d);
        });
    }
}
