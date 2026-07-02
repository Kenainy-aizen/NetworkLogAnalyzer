using Parser;
using Parser.Models;
using Xunit;

namespace Tests.Parser;

public class FirewalldParserTests
{
    private readonly FirewalldParser _parser = new();

    private static readonly string RejectLine =
        "Jol 01 15:16:27 endeavourOs kernel: filter_IN_public_REJECT: IN=wlan0 OUT= MAC=01:00:5e:00:00:fb SRC=192.168.43.1 DST=10.0.0.1 LEN=276 TOS=0x00 PREC=0x00 TTL=255 PROTO=TCP SPT=12345 DPT=80";

    private static readonly string MulticastLine =
        "Jol 01 15:16:27 endeavourOs kernel: filter_IN_public_REJECT: IN=wlan0 OUT= MAC=01:00:5e SRC=192.168.43.1 DST=224.0.0.251 LEN=276 PROTO=UDP SPT=5353 DPT=5353";

    private static readonly string AcceptLine =
        "Jol 01 15:16:27 endeavourOs kernel: filter_IN_public_ACCEPT: IN=wlan0 OUT= MAC= SRC=192.168.1.50 DST=10.0.0.1 LEN=60 PROTO=TCP SPT=54321 DPT=443";

    [Fact]
    public void CanParse_RejectLine_ReturnsTrue()
    {
        Assert.True(_parser.CanParse(RejectLine));
    }

    [Fact]
    public void CanParse_RandomLine_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("Jun 29 20:15:32 mymachine sshd[1234]: some message"));
    }

    [Fact]
    public void Parse_RejectLine_ReturnsBlockEvent()
    {
        var raw = new RawLogLine { Content = RejectLine, Source = "firewalld" };
        var result = _parser.Parse(raw);

        Assert.NotNull(result);
        Assert.Equal("192.168.43.1", result.SourceIp);
        Assert.Equal("10.0.0.1", result.DestinationIp);
        Assert.Equal("TCP", result.Protocol);
        Assert.Equal(80, result.Port);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("WARNING", result.Severity);
        Assert.Equal("firewalld", result.Source);
    }

    [Fact]
    public void Parse_MulticastDestination_ReturnsNull()
    {
        // Le trafic multicast est du bruit réseau normal — on l'ignore
        var raw = new RawLogLine { Content = MulticastLine, Source = "firewalld" };
        var result = _parser.Parse(raw);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_AcceptLine_ReturnsAllowEvent()
    {
        var raw = new RawLogLine { Content = AcceptLine, Source = "firewalld" };
        var result = _parser.Parse(raw);

        Assert.NotNull(result);
        Assert.Equal("ALLOW", result.Action);
        Assert.Equal("INFO", result.Severity);
        Assert.Equal(443, result.Port);
    }
}
