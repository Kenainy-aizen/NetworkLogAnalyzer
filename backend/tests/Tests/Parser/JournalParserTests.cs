using Parser;
using Parser.Models;
using Xunit;

namespace Tests.Parser;

public class JournalParserTests
{
    private readonly JournalParser _parser = new();

    [Fact]
    public void CanParse_ValidJournalLine_ReturnsTrue()
    {
        var line = "Jun 29 20:15:32 mymachine sshd[1234]: Failed password for root from 45.33.32.156 port 22 ssh2";
        Assert.True(_parser.CanParse(line));
    }

    [Fact]
    public void CanParse_EmptyLine_ReturnsFalse()
    {
        Assert.False(_parser.CanParse(""));
    }

    [Fact]
    public void CanParse_IptablesLine_ReturnsFalse()
    {
        // Une ligne iptables brute ne matche pas le format journal (pas de service[pid])
        var line = "UFW BLOCK IN=eth0 SRC=1.2.3.4 DST=5.6.7.8 PROTO=TCP DPT=22";
        Assert.False(_parser.CanParse(line));
    }

    [Fact]
    public void Parse_SshFailedPassword_ReturnsBlockEvent()
    {
        var line = "Jun 29 20:15:32 mymachine sshd[1234]: Failed password for root from 45.33.32.156 port 22 ssh2";
        var raw = new RawLogLine { Content = line, Source = "journalctl" };

        var result = _parser.Parse(raw);

        Assert.NotNull(result);
        Assert.Equal("SSH", result.Protocol);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("WARNING", result.Severity);
        Assert.Equal("45.33.32.156", result.SourceIp);
        Assert.Equal(22, result.Port);
    }

    [Fact]
    public void Parse_SshFailedPassword_InvalidUser_ReturnsBlockEvent()
    {
        var line = "Jun 29 20:15:32 mymachine sshd[1234]: Failed password for invalid user fakeuser from ::1 port 39196 ssh2";
        var raw = new RawLogLine { Content = line, Source = "journalctl" };

        var result = _parser.Parse(raw);

        Assert.NotNull(result);
        Assert.Equal("SSH", result.Protocol);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("localhost", result.SourceIp);
    }

    [Fact]
    public void Parse_SshInvalidUser_ReturnsBlockEvent()
    {
        var line = "Jun 29 20:15:32 mymachine sshd-session[1234]: Invalid user fakeuser from ::1 port 39196";
        var raw = new RawLogLine { Content = line, Source = "journalctl" };

        var result = _parser.Parse(raw);

        Assert.NotNull(result);
        Assert.Equal("SSH", result.Protocol);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("localhost", result.SourceIp);
        Assert.Equal(39196, result.Port);
    }

    [Fact]
    public void Parse_SshAccepted_ReturnsAllowEvent()
    {
        var line = "Jun 29 20:16:10 mymachine sshd[1240]: Accepted password for kelly from 192.168.1.50 port 51234 ssh2";
        var raw = new RawLogLine { Content = line, Source = "journalctl" };

        var result = _parser.Parse(raw);

        Assert.NotNull(result);
        Assert.Equal("SSH", result.Protocol);
        Assert.Equal("ALLOW", result.Action);
        Assert.Equal("INFO", result.Severity);
        Assert.Equal("192.168.1.50", result.SourceIp);
    }

    [Fact]
    public void Parse_SudoCommand_ReturnsSudoEvent()
    {
        var line = "Jun 29 20:15:32 mymachine sudo[5678]: kelly : TTY=pts/0 ; USER=root ; COMMAND=/bin/systemctl";
        var raw = new RawLogLine { Content = line, Source = "journalctl" };

        var result = _parser.Parse(raw);

        Assert.NotNull(result);
        Assert.Equal("SUDO", result.Protocol);
        Assert.Equal("ALLOW", result.Action);
        Assert.Equal("WARNING", result.Severity);
    }

    [Fact]
    public void Parse_UnrecognizedService_ReturnsNull()
    {
        var line = "Jun 29 20:15:32 mymachine nginx[1234]: some nginx log line";
        var raw = new RawLogLine { Content = line, Source = "journalctl" };

        var result = _parser.Parse(raw);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_PreservesSourceField()
    {
        var line = "Jun 29 20:15:32 mymachine sshd[1234]: Failed password for root from 1.2.3.4 port 22 ssh2";
        var raw = new RawLogLine { Content = line, Source = "journalctl" };

        var result = _parser.Parse(raw);

        Assert.Equal("journalctl", result!.Source);
        Assert.Equal(line, result.RawData);
    }
}
