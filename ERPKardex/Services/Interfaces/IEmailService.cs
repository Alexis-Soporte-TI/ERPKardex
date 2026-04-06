namespace ERPKardex.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(List<string> toEmails, string subject, string body, bool isHtml = true);
    }
}
