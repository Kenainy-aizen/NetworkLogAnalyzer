using Parser;
using Parser.Models;
using Xunit;

namespace Tests.Parser;

public class IptablesParserTests
{
    private readonly IptablesParser _parser = new();

    [Fact]
    public void CanParse_UfwBlockLine_ReturnsTrue()
    {
        var line = "Jun 29 20:15:32 mymachine kernel: [UFW BLOCK] IN=eth0 SRC=45.33.32.156 DST=192.168.1.1 PROTO=TCP DPT=22";
        Assert.True(_parser.CanParse(line));
    }

    [Fact]
    public void CanParse_RandomLine_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("Jun 29 20:15:32 mymachine sshd[1234]: some message"));
    }

    [Fact]
    public void Parse_UfwBlockLine_ReturnsBlockEvent()
    {
        var line = "Jun 29 20:15:32 mymachine kernel: [UFW BLOCK] IN=eth0 SRC=45.33.32.156 DST=192.168.1.1 PROTO=TCP DPT=22";
        var raw = new RawLogLine { Content = line, Source = "iptables" };

        var result = _parser.Parse(raw);

        Assert.NotNull(result);
        Assert.Equal("45.33.32.156", result.SourceIp);
        Assert.Equal("192.168.1.1", result.DestinationIp);
        Assert.Equal("TCP", result.Protocol);
        Assert.Equal(22, result.Port);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("WARNING", result.Severity);
    }

    [Fact]
    public void Parse_UfwAllowLine_ReturnsAllowEvent()
    {
        var line = "Jun 29 20:15:32 mymachine kernel: [UFW ALLOW] IN=eth0 SRC=10.0.0.1 DST=192.168.1.1 PROTO=TCP DPT=80";
        var raw = new RawLogLine { Content = line, Source = "iptables" };

        var result = _parser.Parse(raw);

        Assert.NotNull(result);
        Assert.Equal("ALLOW", result.Action);
        Assert.Equal("INFO", result.Severity);
    }

    [Fact]
    public void Parse_LineWithoutPort_ReturnsNullPort()
    {
        var line = "Jun 29 20:15:32 mymachine kernel: UFW BLOCK IN=eth0 SRC=1.2.3.4 DST=5.6.7.8 PROTO=ICMP";
        var raw = new RawLogLine { Content = line, Source = "iptables" };

        var result = _parser.Parse(raw);

        Assert.NotNull(result);
        Assert.Null(result.Port);
        Assert.Equal("ICMP", result.Protocol);
    }
}
