using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Storage;
using Storage.Models;
using Xunit;

namespace Tests.Integration;

public class EventsControllerTests
{
    private HttpClient CreateClient()
    {
        var dbName = "TestDb_" + Guid.NewGuid();

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
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

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));

                    var hosted = services
                        .Where(d => d.ServiceType == typeof(IHostedService))
                        .ToList();
                    foreach (var d in hosted)
                        services.Remove(d);
                });
            });

        return factory.CreateClient();
    }

    [Fact]
    public async Task GetEvents_EmptyDb_ReturnsEmptyList()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<NetworkEvent>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetEvents_AfterInsert_ReturnsEvents()
    {
        var client = CreateClient();
        await CreateEvent(client, "192.168.1.1", "SSH", "BLOCK", "WARNING");

        var response = await client.GetAsync("/api/events");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<NetworkEvent>>();

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetEvents_FilterBySeverity_ReturnsOnlyMatching()
    {
        var client = CreateClient();
        await CreateEvent(client, "1.1.1.1", "SSH", "BLOCK", "WARNING");
        await CreateEvent(client, "2.2.2.2", "SSH", "BLOCK", "CRITICAL");
        await CreateEvent(client, "3.3.3.3", "SUDO", "ALLOW", "INFO");

        var response = await client.GetAsync("/api/events?severity=WARNING");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<NetworkEvent>>();

        Assert.NotNull(result);
        Assert.All(result.Items, e => Assert.Equal("WARNING", e.Severity));
    }

    [Fact]
    public async Task GetEvents_FilterBySourceIp_ReturnsOnlyMatching()
    {
        var client = CreateClient();
        await CreateEvent(client, "10.0.0.1", "SSH", "BLOCK", "WARNING");
        await CreateEvent(client, "10.0.0.2", "SSH", "BLOCK", "WARNING");

        var response = await client.GetAsync("/api/events?sourceIp=10.0.0.1");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<NetworkEvent>>();

        Assert.NotNull(result);
        Assert.All(result.Items, e => Assert.Equal("10.0.0.1", e.SourceIp));
    }

    [Fact]
    public async Task GetEvents_Pagination_ReturnsCorrectPage()
    {
        var client = CreateClient();

        // Insérer 5 événements
        for (int i = 1; i <= 5; i++)
            await CreateEvent(client, $"10.0.0.{i}", "SSH", "BLOCK", "WARNING");

        // Page 1 avec pageSize=2
        var response = await client.GetAsync("/api/events?page=1&pageSize=2");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<NetworkEvent>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNext);
        Assert.False(result.HasPrevious);
    }

    [Fact]
    public async Task GetEvents_Pagination_SecondPage()
    {
        var client = CreateClient();

        for (int i = 1; i <= 5; i++)
            await CreateEvent(client, $"10.0.0.{i}", "SSH", "BLOCK", "WARNING");

        var response = await client.GetAsync("/api/events?page=2&pageSize=2");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<NetworkEvent>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.True(result.HasNext);
        Assert.True(result.HasPrevious);
    }

    [Fact]
    public async Task GetEventById_ExistingId_ReturnsEvent()
    {
        var client = CreateClient();
        var created = await CreateEvent(client, "5.5.5.5", "TCP", "BLOCK", "WARNING");
        Assert.NotNull(created);

        var response = await client.GetAsync($"/api/events/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ev = await response.Content.ReadFromJsonAsync<NetworkEvent>();
        Assert.NotNull(ev);
        Assert.Equal("5.5.5.5", ev.SourceIp);
    }

    [Fact]
    public async Task GetEventById_NonExistingId_Returns404()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/events/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStats_ReturnsCorrectCounts()
    {
        var client = CreateClient();
        await CreateEvent(client, "1.1.1.1", "SSH", "BLOCK", "WARNING");
        await CreateEvent(client, "2.2.2.2", "SSH", "BLOCK", "WARNING");
        await CreateEvent(client, "3.3.3.3", "SSH", "BLOCK", "CRITICAL");

        var response = await client.GetAsync("/api/events/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.NotNull(stats);
        Assert.True(stats.ContainsKey("WARNING"));
        Assert.Equal(2, stats["WARNING"]);
        Assert.True(stats.ContainsKey("CRITICAL"));
        Assert.Equal(1, stats["CRITICAL"]);
    }

    [Fact]
    public async Task PostEvent_ValidEvent_Returns201()
    {
        var client = CreateClient();
        var newEvent = new NetworkEvent
        {
            SourceIp  = "192.168.1.50",
            Protocol  = "SSH",
            Port      = 22,
            Action    = "BLOCK",
            Severity  = "WARNING",
            RawData   = "test integration",
            Source    = "test",
        };

        var response = await client.PostAsJsonAsync("/api/events", newEvent);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<NetworkEvent>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("192.168.1.50", created.SourceIp);
    }

    [Fact]
    public async Task PostEvent_ThenGet_ReturnsCreatedEvent()
    {
        var client = CreateClient();
        var newEvent = new NetworkEvent
        {
            SourceIp  = "172.16.0.1",
            Protocol  = "FTP",
            Port      = 21,
            Action    = "BLOCK",
            Severity  = "WARNING",
            RawData   = "ftp test",
            Source    = "vsftpd",
        };

        var postResponse = await client.PostAsJsonAsync("/api/events", newEvent);
        var created = await postResponse.Content.ReadFromJsonAsync<NetworkEvent>();

        var getResponse = await client.GetAsync($"/api/events/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<NetworkEvent>();

        Assert.NotNull(fetched);
        Assert.Equal("FTP", fetched.Protocol);
        Assert.Equal("vsftpd", fetched.Source);
    }

    private static async Task<NetworkEvent?> CreateEvent(
        HttpClient client, string ip, string protocol, string action, string severity)
    {
        var ev = new NetworkEvent
        {
            SourceIp  = ip,
            Protocol  = protocol,
            Action    = action,
            Severity  = severity,
            RawData   = $"test {ip}",
            Source    = "test",
        };
        var response = await client.PostAsJsonAsync("/api/events", ev);
        return await response.Content.ReadFromJsonAsync<NetworkEvent>();
    }
}
