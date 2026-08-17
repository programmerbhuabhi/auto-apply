using Mail.Models;

namespace Mail.Services
{
    public interface IGeminiService
    {
        Task<GeminiDraftResult> GenerateMailDraftAsync(IFormFile image, string resumeText, string promptText);
    }
}
