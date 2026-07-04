using Parser;
using Parser.Models;
using Xunit;

namespace Tests.Parser;

public class Fail2banParserTests
{
    private readonly Fail2banParser _parser = new();

    private static RawLogLine MakeLine(string content) =>
        new() { Content = content, Source = "fail2ban" };

    [Fact]
    public void CanParse_BanLine_ReturnsTrue()
    {
        var line = "2026-07-03 20:35:08,400 fail2ban.actions [22093]: NOTICE  [sshd] Ban 45.33.32.156";
        Assert.True(_parser.CanParse(line));
    }

    [Fact]
    public void CanParse_FoundLine_ReturnsTrue()
    {
        var line = "2026-07-03 20:35:04,093 fail2ban.filter  [22093]: INFO    [sshd] Found 45.33.32.156 - 2026-07-03 20:35:03";
        Assert.True(_parser.CanParse(line));
    }

    [Fact]
    public void CanParse_InfoLine_ReturnsFalse()
    {
        var line = "2026-07-03 20:34:31,107 fail2ban.jail [22093]: INFO    Jail 'sshd' started";
        Assert.False(_parser.CanParse(line));
    }

    [Fact]
    public void Parse_BanLine_ReturnsCriticalBlock()
    {
        var line = "2026-07-03 20:35:08,400 fail2ban.actions [22093]: NOTICE  [sshd] Ban 45.33.32.156";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("45.33.32.156", result.SourceIp);
        Assert.Equal("FAIL2BAN", result.Protocol);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("CRITICAL", result.Severity);
        Assert.Equal("fail2ban", result.Source);
    }

    [Fact]
    public void Parse_UnbanLine_ReturnsInfoAllow()
    {
        var line = "2026-07-03 20:36:08,400 fail2ban.actions [22093]: NOTICE  [sshd] Unban 45.33.32.156";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("ALLOW", result.Action);
        Assert.Equal("INFO", result.Severity);
    }

    [Fact]
    public void Parse_FoundLine_ReturnsWarningBlock()
    {
        var line = "2026-07-03 20:35:04,093 fail2ban.filter  [22093]: INFO    [sshd] Found 45.33.32.156 - 2026-07-03 20:35:03";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("45.33.32.156", result.SourceIp);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("WARNING", result.Severity);
    }

    [Fact]
    public void Parse_LocalIp_ReturnsLocalhost()
    {
        var line = "2026-07-03 20:35:08,400 fail2ban.actions [22093]: NOTICE  [sshd] Ban ::1";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("localhost", result.SourceIp);
        Assert.Equal("CRITICAL", result.Severity);
    }
}
