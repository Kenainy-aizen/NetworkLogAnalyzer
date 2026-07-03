using Parser;
using Parser.Models;
using Xunit;

namespace Tests.Parser;

public class VsftpdParserTests
{
    private readonly VsftpdParser _parser = new();

    private static RawLogLine MakeLine(string content) =>
        new() { Content = content, Source = "vsftpd" };

    [Fact]
    public void CanParse_FailLoginLine_ReturnsTrue()
    {
        var line = "Fri Jul  3 19:31:53 2026 [pid 1] [kkk] FAIL LOGIN: Client \"127.0.0.1\"";
        Assert.True(_parser.CanParse(line));
    }

    [Fact]
    public void CanParse_RandomLine_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("Jun 29 20:15:32 mymachine sshd[1234]: some message"));
    }

    [Fact]
    public void Parse_FailLogin_ReturnsBlockEvent()
    {
        var line = "Fri Jul  3 19:31:53 2026 [pid 1] [kkk] FAIL LOGIN: Client \"45.33.32.156\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("FTP", result.Protocol);
        Assert.Equal("BLOCK", result.Action);
        Assert.Equal("WARNING", result.Severity);
        Assert.Equal("45.33.32.156", result.SourceIp);
        Assert.Equal(21, result.Port);
        Assert.Equal("vsftpd", result.Source);
    }

    [Fact]
    public void Parse_FailLogin_LocalIp_ReturnsLocalhost()
    {
        var line = "Fri Jul  3 19:31:53 2026 [pid 1] [kkk] FAIL LOGIN: Client \"127.0.0.1\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("localhost", result.SourceIp);
    }

    [Fact]
    public void Parse_OkLogin_ReturnsAllowEvent()
    {
        var line = "Fri Jul  3 19:31:53 2026 [pid 1] [kelly] OK LOGIN: Client \"192.168.1.50\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("FTP", result.Protocol);
        Assert.Equal("ALLOW", result.Action);
        Assert.Equal("INFO", result.Severity);
        Assert.Equal("192.168.1.50", result.SourceIp);
    }

    [Fact]
    public void Parse_OkUpload_NormalFile_ReturnsInfo()
    {
        var line = "Fri Jul  3 19:31:53 2026 [pid 1] [kelly] OK UPLOAD: Client \"192.168.1.50\", \"/home/kelly/document.pdf\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("FTP UPLOAD", result.Protocol);
        Assert.Equal("INFO", result.Severity);
    }

    [Fact]
    public void Parse_OkUpload_SuspiciousFile_ReturnsCritical()
    {
        var line = "Fri Jul  3 19:31:53 2026 [pid 1] [kelly] OK UPLOAD: Client \"192.168.1.50\", \"/var/www/shell.php\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("FTP UPLOAD", result.Protocol);
        Assert.Equal("CRITICAL", result.Severity);
    }

    [Fact]
    public void Parse_OkDownload_ReturnsInfo()
    {
        var line = "Fri Jul  3 19:31:53 2026 [pid 1] [kelly] OK DOWNLOAD: Client \"192.168.1.50\", \"/home/kelly/file.txt\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.NotNull(result);
        Assert.Equal("FTP DOWNLOAD", result.Protocol);
        Assert.Equal("INFO", result.Severity);
    }

    [Fact]
    public void Parse_ConnectOnly_ReturnsNull()
    {
        var line = "Fri Jul  3 19:31:39 2026 [pid 2] CONNECT: Client \"127.0.0.1\"";
        var result = _parser.Parse(MakeLine(line));

        Assert.Null(result);
    }
}
