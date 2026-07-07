namespace Api.Services;

public interface IEmailService
{
    Task SendAlertAsync(string subject, string body);
}
