namespace Mail.Models
{
    public class GenerateMailRequest
    {
        public IFormFile? Image { get; set; }
        public string? AccessToken { get; set; }
    }

    public class GenerateMailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? HrEmail { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public string? ResumeFileName { get; set; }
    }

    public class SendMailRequest
    {
        public string HrEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? SenderEmail { get; set; }
        public string? AppPassword { get; set; }
        public string? AccessToken { get; set; }
    }

    public class SendMailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class GeminiDraftResult
    {
        public string HrEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
