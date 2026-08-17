namespace Mail.Services
{
    public interface IEmailService
    {
        Task SendEmailWithAttachmentAsync(
            string toEmail,
            string subject,
            string body,
            string? attachmentPath = null,
            string? overrideSenderEmail = null,
            string? overrideAppPassword = null);
    }
}
