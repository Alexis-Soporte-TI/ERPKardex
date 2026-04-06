using ERPKardex.Models;
using ERPKardex.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ERPKardex.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        // Usamos IOptions para inyectar la configuración del appsettings.json
        public EmailService(IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(List<string> toEmails, string subject, string body, bool isHtml = true)
        {
            var email = new MimeMessage();

            // Remitente
            email.From.Add(new MailboxAddress(_settings.FromName, _settings.From));

            // Destinatarios (recorremos la lista que pasas como parámetro)
            foreach (var toAddress in toEmails)
            {
                email.To.Add(MailboxAddress.Parse(toAddress));
            }

            email.Subject = subject;

            // Cuerpo del mensaje
            var builder = new BodyBuilder();
            if (isHtml)
            {
                builder.HtmlBody = body;
            }
            else
            {
                builder.TextBody = body;
            }

            email.Body = builder.ToMessageBody();

            // Configuración del cliente SMTP
            using var smtp = new SmtpClient();

            // Conexión usando StartTls, que es el estándar para el puerto 587 (Gmail/Outlook)
            await smtp.ConnectAsync(_settings.Server, _settings.Port, SecureSocketOptions.StartTls);

            // Autenticación
            await smtp.AuthenticateAsync(_settings.User, _settings.Pass);

            // Envío y desconexión
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
