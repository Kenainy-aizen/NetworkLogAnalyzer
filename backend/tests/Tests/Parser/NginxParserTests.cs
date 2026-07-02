using Parser;
using Parser.Models;
using Xunit;

namespace Tests.Parser;

public class NginxParserTests
{
    private readonly NginxParser _parser = new();

    private static RawLogLine MakeLine(string content) =>
        new() { Content = content, Source = "nginx" };

    [Fact]
    public void CanParse_ValidNginxLine_ReturnsTrue()
    {
        var line = "127.0.0.1 - - [02/Jul/2026:11:37:55 +0300] \"GET / HTTP/1.1\" 200 896 \"-\" \"curl/8.21.0\"";
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
        // Requête normale GET / → on ignore
        var line = "127.0.0.1 - - [02/Jul/2026:11:37:55 +0300] \"GET / HTTP/1.1\" 200 896 \"-\" \"curl/8.21.0\"";
        var result = _parser.Parse(MakeLine(line));
        Assert.Null(result);
    }

    [Fact]
    public void Parse_SuspiciousPath_ReturnsWarning()
    {
        var line = "45.33.32.156 - - [02/Jul/2026:11:37:55 +0300] \"GET /wp-login.php HTTP/1.1\" 404 153 \"-\" \"Mozilla/5.0\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("WARNING", result.Severity);
        Assert.Equal("45.33.32.156", result.SourceIp);
        Assert.Equal("BLOCK", result.Action);
    }

    [Fact]
    public void Parse_EnvFileScan_ReturnsWarning()
    {
        var line = "10.0.0.1 - - [02/Jul/2026:11:37:55 +0300] \"GET /.env HTTP/1.1\" 404 153 \"-\" \"python-requests/2.28\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("WARNING", result.Severity);
        Assert.Equal("10.0.0.1", result.SourceIp);
    }

    [Fact]
    public void Parse_PathTraversal_ReturnsCritical()
    {
        var line = "192.168.1.5 - - [02/Jul/2026:11:37:55 +0300] \"GET /../../etc/passwd HTTP/1.1\" 400 153 \"-\" \"curl/8.0\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("CRITICAL", result.Severity);
    }

    [Fact]
    public void Parse_PhpExtension_ReturnsWarning()
    {
        var line = "1.2.3.4 - - [02/Jul/2026:11:37:55 +0300] \"GET /shell.php HTTP/1.1\" 404 153 \"-\" \"curl/8.0\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("WARNING", result.Severity);
    }

    [Fact]
    public void Parse_ServerError_ReturnsWarning()
    {
        var line = "127.0.0.1 - - [02/Jul/2026:11:37:55 +0300] \"GET /api/crash HTTP/1.1\" 500 153 \"-\" \"curl/8.0\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("WARNING", result.Severity);
    }

    [Fact]
    public void Parse_PreservesSourceIp()
    {
        var line = "203.0.113.42 - - [02/Jul/2026:11:37:55 +0300] \"GET /admin HTTP/1.1\" 404 153 \"-\" \"bot/1.0\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("203.0.113.42", result.SourceIp);
        Assert.Equal("nginx", result.Source);
    }
}
