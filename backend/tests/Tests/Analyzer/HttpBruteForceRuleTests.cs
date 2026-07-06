using Analyzer.Rules;
using Storage.Models;
using Xunit;

namespace Tests.Analyzer;

public class HttpBruteForceRuleTests
{
    private readonly HttpBruteForceRule _rule = new();

    private static NetworkEvent MakeHttpBlock(string ip, string path = "/login", int secondsAgo = 0) => new()
    {
        SourceIp  = ip,
        Protocol  = "HTTP POST",
        Action    = "BLOCK",
        Severity  = "WARNING",
        Source    = "nginx",
        Timestamp = DateTime.UtcNow.AddSeconds(-secondsAgo),
        RawData   = $"1.2.3.4 - - [01/Jul/2026] \"POST {path} HTTP/1.1\" 401 153",
    };

    [Fact]
    public async Task NoAlert_WhenBelowThreshold()
    {
        var newEvent = MakeHttpBlock("1.2.3.4");
        var history  = Enumerable.Range(0, 9).Select(i => MakeHttpBlock("1.2.3.4", "/login", i));

        var alert = await _rule.EvaluateAsync(newEvent, history);
        Assert.Null(alert);
    }

    [Fact]
    public async Task Alert_WhenThresholdReached()
    {
        var newEvent = MakeHttpBlock("5.5.5.5");
        var history  = Enumerable.Range(0, 10).Select(i => MakeHttpBlock("5.5.5.5", "/login", i));

        var alert = await _rule.EvaluateAsync(newEvent, history);
        Assert.NotNull(alert);
        Assert.Equal("CRITICAL", alert.Severity);
        Assert.Equal("ALERT", alert.Protocol);
    }

    [Fact]
    public async Task NoAlert_WhenNotLoginPath()
    {
        var newEvent = MakeHttpBlock("1.2.3.4", "/.env");
        var history  = Enumerable.Range(0, 10).Select(i => MakeHttpBlock("1.2.3.4", "/.env", i));

        var alert = await _rule.EvaluateAsync(newEvent, history);
        Assert.Null(alert);
    }

    [Fact]
    public async Task NoAlert_WhenDifferentIp()
    {
        var newEvent = MakeHttpBlock("1.2.3.4");
        var history  = Enumerable.Range(0, 15).Select(i => MakeHttpBlock("9.9.9.9", "/login", i));

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
            RawData   = "/login attempt",
        };
        var history = Enumerable.Range(0, 15).Select(i => MakeHttpBlock("1.2.3.4", "/login", i));

        var alert = await _rule.EvaluateAsync(newEvent, history);
        Assert.Null(alert);
    }
}
