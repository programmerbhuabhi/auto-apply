using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Mail.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailWithAttachmentAsync(
            string toEmail,
            string subject,
            string body,
            string? attachmentPath = null,
            string? overrideSenderEmail = null,
            string? overrideAppPassword = null)
        {
            string host = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
            int port = int.Parse(_configuration["Smtp:Port"] ?? "587");

            // Use override email/password if provided, otherwise default to appsettings.json
            string username = !string.IsNullOrWhiteSpace(overrideSenderEmail)
                ? overrideSenderEmail.Trim()
                : (_configuration["Smtp:Username"] ?? "abhishekanandmahto77@gmail.com");

            string rawPassword = !string.IsNullOrWhiteSpace(overrideAppPassword)
                ? overrideAppPassword
                : (_configuration["Smtp:Password"] ?? "hzup ipiv piva sxyz");

            string password = rawPassword.Trim().Replace(" ", "");
            //string password = rawPassword;
            string fromAddress = username;
            string fromName = _configuration["Smtp:FromName"] ?? "Abhishek Anand Mahto";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                TextBody = body
            };

            if (!string.IsNullOrWhiteSpace(attachmentPath) && File.Exists(attachmentPath))
            {
                builder.Attachments.Add(attachmentPath);
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            // Accept SSL certificates for secure connection
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            try
            {
                _logger.LogInformation("Connecting to SMTP host {Host}:{Port}...", host, port);
                
                var secureOption = (port == 465) ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                await client.ConnectAsync(host, port, secureOption);

                _logger.LogInformation("Authenticating with SMTP server as {Username}...", username);
                await client.AuthenticateAsync(username, password);

                _logger.LogInformation("Sending email to {ToEmail}...", toEmail);
                await client.SendAsync(message);
                
                await client.DisconnectAsync(true);
                _logger.LogInformation("Email sent successfully to {ToEmail}.", toEmail);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _logger.LogError(ex, "SMTP Authentication Failed for user {Username}", username);
                throw new Exception($"Invalid Gmail credentials for {username}. Please generate a new 16-character App Password at https://myaccount.google.com/apppasswords and enter it in the app settings.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP Error sending email.");
                throw;
            }
        }
    }
}
