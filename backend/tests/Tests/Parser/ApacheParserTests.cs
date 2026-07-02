using Parser;
using Parser.Models;
using Xunit;

namespace Tests.Parser;

public class ApacheParserTests
{
    private readonly ApacheParser _parser = new();

    private static RawLogLine MakeLine(string content) =>
        new() { Content = content, Source = "apache" };

    [Fact]
    public void CanParse_ValidApacheLine_ReturnsTrue()
    {
        var line = "::1 - - [02/Jul/2026:12:14:38 +0300] \"GET / HTTP/1.1\" 200 2232";
        Assert.True(_parser.CanParse(line));
    }

    [Fact]
    public void CanParse_JournalLine_ReturnsFalse()
    {
        var line = "Jun 29 20:15:32 mymachine sshd[1234]: some message";
        Assert.False(_parser.CanParse(line));
    }

    [Fact]
    public void Parse_NormalRequest_ReturnsNull()
    {
        var line = "::1 - - [02/Jul/2026:12:14:38 +0300] \"GET / HTTP/1.1\" 200 2232";
        var result = _parser.Parse(MakeLine(line));
        Assert.Null(result);
    }

    [Fact]
    public void Parse_AdminPath_ReturnsWarning()
    {
        var line = "::1 - - [02/Jul/2026:12:14:39 +0300] \"GET /admin HTTP/1.1\" 404 997";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("WARNING", result.Severity);
        Assert.Equal("::1", result.SourceIp);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("apache", result.Source);
    }

    [Fact]
    public void Parse_EnvFile_ReturnsWarning()
    {
        var line = "45.33.32.156 - - [02/Jul/2026:12:14:39 +0300] \"GET /.env HTTP/1.1\" 404 997";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("WARNING", result.Severity);
        Assert.Equal("45.33.32.156", result.SourceIp);
    }

    [Fact]
    public void Parse_PathTraversal_ReturnsCritical()
    {
        var line = "1.2.3.4 - - [02/Jul/2026:12:14:39 +0300] \"GET /../../etc/passwd HTTP/1.1\" 400 997";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("CRITICAL", result.Severity);
    }

    [Fact]
    public void Parse_PhpShell_ReturnsWarning()
    {
        var line = "10.0.0.5 - - [02/Jul/2026:12:14:39 +0300] \"GET /shell.php HTTP/1.1\" 404 997";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("WARNING", result.Severity);
    }

    [Fact]
    public void Parse_ServerError_ReturnsWarning()
    {
        var line = "127.0.0.1 - - [02/Jul/2026:12:14:39 +0300] \"GET /crash HTTP/1.1\" 500 997";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("WARNING", result.Severity);
    }

    [Fact]
    public void Parse_Ipv6Source_ParsedCorrectly()
    {
        var line = "::1 - - [02/Jul/2026:12:14:39 +0300] \"GET /wp-login.php HTTP/1.1\" 404 997";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("::1", result.SourceIp);
        Assert.Equal("HTTP GET", result.Protocol);
        Assert.Equal(80, result.Port);
    }
}
