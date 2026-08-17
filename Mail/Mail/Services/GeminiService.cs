using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mail.Models;

namespace Mail.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<GeminiDraftResult> GenerateMailDraftAsync(IFormFile image, string resumeText, string promptText)
        {
            string apiKey = _configuration["Gemini:ApiKey"] ?? string.Empty;
            string model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
            {
                _logger.LogWarning("Gemini API key is not configured in appsettings.json. Falling back to local OCR extraction parser.");
                return FallbackAnalysis(resumeText);
            }

            try
            {
                // Convert image to base64
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                byte[] imageBytes = ms.ToArray();
                string base64Image = Convert.ToBase64String(imageBytes);
                string mimeType = string.IsNullOrWhiteSpace(image.ContentType) ? "image/jpeg" : image.ContentType;

                string fullPrompt = $"{promptText}\n\n=== CANDIDATE RESUME ===\n{resumeText}";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new
                                {
                                    inlineData = new
                                    {
                                        mimeType = mimeType,
                                        data = base64Image
                                    }
                                },
                                new
                                {
                                    text = fullPrompt
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json"
                    }
                };

                string requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API call failed with status {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);
                    return FallbackAnalysis(resumeText);
                }

                using var doc = JsonDocument.Parse(responseBody);
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        // Clean markdown json fences if returned
                        string cleanJson = text.Trim();
                        if (cleanJson.StartsWith("```json"))
                        {
                            cleanJson = cleanJson.Substring(7);
                        }
                        if (cleanJson.StartsWith("```"))
                        {
                            cleanJson = cleanJson.Substring(3);
                        }
                        if (cleanJson.EndsWith("```"))
                        {
                            cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                        }
                        cleanJson = cleanJson.Trim();

                        var result = JsonSerializer.Deserialize<GeminiDraftResult>(cleanJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (result != null)
                        {
                            if (string.IsNullOrWhiteSpace(result.HrEmail)) result.HrEmail = "hr@company.com";
                            if (string.IsNullOrWhiteSpace(result.Subject)) result.Subject = "Application for Open Role - Abhishek Anand Mahto";
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Gemini API call.");
            }

            return FallbackAnalysis(resumeText);
        }

        private static GeminiDraftResult FallbackAnalysis(string resumeText)
        {
            return new GeminiDraftResult
            {
                HrEmail = "hr@company.com",
                Subject = "Application for Software Engineering Role - Abhishek Anand Mahto",
                Body = $"Dear Hiring Manager,\n\nI am writing to express my strong interest in the opportunity advertised in your job posting. Based on my technical background in .NET, C#, web API development, and cloud services, I am confident in my ability to add immediate value to your team.\n\nKey Highlights from my Profile:\n- Strong expertise in ASP.NET Core, C#, Web API, SQL Server, and Modern Web Services.\n- Proven track record building secure, scalable microservices and user interfaces.\n- Solid experience in REST API integrations, performance optimization, and clean architecture.\n\nPlease find my resume attached for your review. I would welcome the opportunity to discuss how my technical skills align with your team's goals.\n\nThank you for your time and consideration.\n\nSincerely,\nAbhishek Anand Mahto\nabhisheksvml@gmail.com"
            };
        }
    }
}
