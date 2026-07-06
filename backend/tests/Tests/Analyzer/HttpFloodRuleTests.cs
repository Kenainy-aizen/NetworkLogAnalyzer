using Analyzer.Rules;
using Storage.Models;
using Xunit;

namespace Tests.Analyzer;

public class HttpFloodRuleTests
{
    private readonly HttpFloodRule _rule = new();

    private static NetworkEvent MakeHttpEvent(string ip, int secondsAgo = 0) => new()
    {
        SourceIp  = ip,
        Protocol  = "HTTP GET",
        Action    = "ALLOW",
        Severity  = "INFO",
        Source    = "nginx",
        Timestamp = DateTime.UtcNow.AddSeconds(-secondsAgo),
        RawData   = $"{ip} - - \"GET / HTTP/1.1\" 200 1234",
    };

    [Fact]
    public async Task NoAlert_WhenBelowThreshold()
    {
        var newEvent = MakeHttpEvent("1.2.3.4");
        var history  = Enumerable.Range(0, 99).Select(i => MakeHttpEvent("1.2.3.4", i));

        var alert = await _rule.EvaluateAsync(newEvent, history);
        Assert.Null(alert);
    }

    [Fact]
    public async Task Alert_WhenThresholdReached()
    {
        var newEvent = MakeHttpEvent("6.6.6.6");
        var history  = Enumerable.Range(0, 100).Select(i => MakeHttpEvent("6.6.6.6", i % 10));

        var alert = await _rule.EvaluateAsync(newEvent, history);
        Assert.NotNull(alert);
        Assert.Equal("CRITICAL", alert.Severity);
        Assert.Contains("Flood", alert.RawData);
    }

    [Fact]
    public async Task NoAlert_WhenDifferentIp()
    {
        var newEvent = MakeHttpEvent("1.2.3.4");
        var history  = Enumerable.Range(0, 150).Select(i => MakeHttpEvent("9.9.9.9", i));

        var alert = await _rule.EvaluateAsync(newEvent, history);
        Assert.Null(alert);
    }

    [Fact]
    public async Task NoAlert_WhenNotHttpSource()
    {
        var newEvent = new NetworkEvent
        {
            SourceIp  = "1.2.3.4",
            Protocol  = "SSH",
            Action    = "BLOCK",
            Source    = "journalctl",
            Timestamp = DateTime.UtcNow,
            RawData   = "test",
        };
        var history = Enumerable.Range(0, 150).Select(i => MakeHttpEvent("1.2.3.4", i));

        var alert = await _rule.EvaluateAsync(newEvent, history);
        Assert.Null(alert);
    }

    [Fact]
    public async Task NoAlert_WhenEventsOutsideWindow()
    {
        var newEvent = MakeHttpEvent("1.2.3.4");
        // Événements il y a 5 minutes (hors fenêtre de 30 secondes)
        var history  = Enumerable.Range(0, 150).Select(i => MakeHttpEvent("1.2.3.4", 300 + i));

        var alert = await _rule.EvaluateAsync(newEvent, history);
        Assert.Null(alert);
    }
}
