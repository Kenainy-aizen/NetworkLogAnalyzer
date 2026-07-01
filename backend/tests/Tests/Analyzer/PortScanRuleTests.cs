using Analyzer.Rules;
using Storage.Models;
using Xunit;

namespace Tests.Analyzer;

public class PortScanRuleTests
{
    private readonly PortScanRule _rule = new();

    private static NetworkEvent MakeTcpBlock(string ip, int port, int secondsAgo = 0) => new()
    {
        SourceIp  = ip,
        Protocol  = "TCP",
        Action    = "BLOCK",
        Port      = port,
        Severity  = "WARNING",
        Timestamp = DateTime.UtcNow.AddSeconds(-secondsAgo),
        RawData   = "test",
        Source    = "test",
    };

    [Fact]
    public async Task NoAlert_WhenBelowPortThreshold()
    {
        var newEvent = MakeTcpBlock("1.2.3.4", 80);
        var history  = Enumerable.Range(1, 9).Select(i => MakeTcpBlock("1.2.3.4", i, i));

        var alert = await _rule.EvaluateAsync(newEvent, history);

        Assert.Null(alert);
    }

    [Fact]
    public async Task Alert_WhenDistinctPortThresholdReached()
    {
        var newEvent = MakeTcpBlock("1.2.3.4", 11);
        var history  = Enumerable.Range(1, 10).Select(i => MakeTcpBlock("1.2.3.4", i, i));

        var alert = await _rule.EvaluateAsync(newEvent, history);

        Assert.NotNull(alert);
        Assert.Equal("ALERT", alert.Protocol);
        Assert.Equal("CRITICAL", alert.Severity);
    }

    [Fact]
    public async Task NoAlert_WhenSamePortRepeated()
    {
        var newEvent = MakeTcpBlock("1.2.3.4", 22);
        // 20 tentatives mais toujours le même port
        var history = Enumerable.Range(0, 20).Select(i => MakeTcpBlock("1.2.3.4", 22, i));

        var alert = await _rule.EvaluateAsync(newEvent, history);

        Assert.Null(alert);
    }

    [Fact]
    public async Task NoAlert_WhenDifferentIp()
    {
        var newEvent = MakeTcpBlock("1.2.3.4", 80);
        var history  = Enumerable.Range(1, 15).Select(i => MakeTcpBlock("9.9.9.9", i, i));

        var alert = await _rule.EvaluateAsync(newEvent, history);

        Assert.Null(alert);
    }

    [Fact]
    public async Task NoAlert_WhenPortIsNull()
    {
        var newEvent = new NetworkEvent
        {
            SourceIp  = "1.2.3.4",
            Protocol  = "TCP",
            Action    = "BLOCK",
            Port      = null,
            Timestamp = DateTime.UtcNow,
            RawData   = "test",
            Source    = "test",
        };
        var history = Enumerable.Range(1, 15).Select(i => MakeTcpBlock("1.2.3.4", i, i));

        var alert = await _rule.EvaluateAsync(newEvent, history);

        Assert.Null(alert);
    }
}
