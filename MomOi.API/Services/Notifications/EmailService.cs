using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace MomOi.API.Services.Notifications
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var host = _config["Smtp:Host"] ?? "smtp.gmail.com";
            var portString = _config["Smtp:Port"] ?? "587";
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];
            var enableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("SMTP credentials are not configured. Skipping email to {ToEmail}", toEmail);
                return;
            }

            if (!int.TryParse(portString, out int port)) port = 587;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("MomBaby Care", username));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlMessage
                };
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                var secureSocketOptions = enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                
                await client.ConnectAsync(host, port, secureSocketOptions);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            }
        }
    }
}
