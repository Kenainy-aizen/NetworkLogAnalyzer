using Parser;
using Parser.Models;
using Xunit;

namespace Tests.Parser;

public class PamParserTests
{
    private readonly PamParser _parser = new();

    private static RawLogLine MakeLine(string content) =>
        new() { Content = content, Source = "journalctl" };

    [Fact]
    public void CanParse_SuFailedLine_ReturnsTrue()
    {
        var line = "Jol 04 13:06:29 endeavourOs su[5748]: FAILED SU (to root) kenainy on pts/1";
        Assert.True(_parser.CanParse(line));
    }

    [Fact]
    public void CanParse_PamAuthFailure_ReturnsTrue()
    {
        var line = "Jol 04 13:06:26 endeavourOs su[5748]: pam_unix(su:auth): authentication failure; logname=kenainy uid=1000 euid=0 tty=/dev/pts/1 ruser=kenainy rhost=  user=root";
        Assert.True(_parser.CanParse(line));
    }

    [Fact]
    public void CanParse_RandomLine_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("Jun 29 20:15:32 mymachine sshd[1234]: some message"));
    }

    [Fact]
    public void Parse_SuFailed_ReturnsWarningBlock()
    {
        var line = "Jol 04 13:06:29 endeavourOs su[5748]: FAILED SU (to root) kenainy on pts/1";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("SU", result.Protocol);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("WARNING", result.Severity);
        Assert.Equal("localhost", result.SourceIp);
        Assert.Equal("pam", result.Source);
    }

    [Fact]
    public void Parse_PamAuthFailure_ReturnsWarningBlock()
    {
        var line = "Jol 04 13:06:26 endeavourOs su[5748]: pam_unix(su:auth): authentication failure; logname=kenainy uid=1000 euid=0 tty=/dev/pts/1 ruser=kenainy rhost=  user=root";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("SU", result.Protocol);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("WARNING", result.Severity);
    }

    [Fact]
    public void Parse_SuSuccessToRoot_ReturnsWarningAllow()
    {
        var line = "Jol 04 13:06:29 endeavourOs su[5748]: Successful su for root by kenainy";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("SU", result.Protocol);
        Assert.Equal("ALLOW", result.Action);
        Assert.Equal("WARNING", result.Severity);
    }

    [Fact]
    public void Parse_SuSuccessToUser_ReturnsInfoAllow()
    {
        var line = "Jol 04 13:06:29 endeavourOs su[5748]: Successful su for kelly by kenainy";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("SU", result.Protocol);
        Assert.Equal("ALLOW", result.Action);
        Assert.Equal("INFO", result.Severity);
    }

    [Fact]
    public void Parse_LoginFailed_ReturnsWarningBlock()
    {
        var line = "Jol 04 13:06:29 endeavourOs login[5748]: FAILED LOGIN (1) on 'tty1' FOR 'fakeuser'";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("LOGIN", result.Protocol);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("WARNING", result.Severity);
    }
}
