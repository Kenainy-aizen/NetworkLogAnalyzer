using Analyzer.Rules;
using Storage.Models;
using Xunit;

namespace Tests.Analyzer;

public class SshBruteForceRuleTests
{
    private readonly SshBruteForceRule _rule = new();

    private static NetworkEvent MakeSshBlock(string ip, int secondsAgo = 0) => new()
    {
        SourceIp  = ip,
        Protocol  = "SSH",
        Action    = "BLOCK",
        Severity  = "WARNING",
        Timestamp = DateTime.UtcNow.AddSeconds(-secondsAgo),
        RawData   = "test",
        Source    = "journalctl",
    };

    [Fact]
    public async Task NoAlert_WhenBelowThreshold()
    {
        var newEvent = MakeSshBlock("1.2.3.4");
        var history  = Enumerable.Range(0, 4).Select(i => MakeSshBlock("1.2.3.4", i));

        var alert = await _rule.EvaluateAsync(newEvent, history);

        Assert.Null(alert);
    }

    [Fact]
    public async Task Alert_WhenThresholdReached()
    {
        var newEvent = MakeSshBlock("1.2.3.4");
        var history  = Enumerable.Range(0, 5).Select(i => MakeSshBlock("1.2.3.4", i));

        var alert = await _rule.EvaluateAsync(newEvent, history);

        Assert.NotNull(alert);
        Assert.Equal("ALERT", alert.Protocol);
        Assert.Equal("CRITICAL", alert.Severity);
        Assert.Equal("1.2.3.4", alert.SourceIp);
    }

    [Fact]
    public async Task NoAlert_WhenDifferentIp()
    {
        var newEvent = MakeSshBlock("1.2.3.4");
        // 10 tentatives depuis une IP différente
        var history = Enumerable.Range(0, 10).Select(i => MakeSshBlock("9.9.9.9", i));

        var alert = await _rule.EvaluateAsync(newEvent, history);

        Assert.Null(alert);
    }

    [Fact]
    public async Task NoAlert_WhenEventsOutsideWindow()
    {
        var newEvent = MakeSshBlock("1.2.3.4");
        // 10 tentatives mais il y a 5 minutes (hors fenêtre d'1 minute)
        var history = Enumerable.Range(0, 10).Select(i => MakeSshBlock("1.2.3.4", 300 + i));

        var alert = await _rule.EvaluateAsync(newEvent, history);

        Assert.Null(alert);
    }

    [Fact]
    public async Task NoAlert_WhenProtocolIsNotSsh()
    {
        var newEvent = new NetworkEvent
        {
            SourceIp  = "1.2.3.4",
            Protocol  = "TCP",
            Action    = "BLOCK",
            Timestamp = DateTime.UtcNow,
            RawData   = "test",
            Source    = "test",
        };
        var history = Enumerable.Range(0, 10).Select(i => MakeSshBlock("1.2.3.4", i));

        var alert = await _rule.EvaluateAsync(newEvent, history);

        Assert.Null(alert);
    }
}
