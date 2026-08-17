using Microsoft.AspNetCore.Mvc;
using Mail.Models;
using Mail.Services;

namespace Mail.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MailController : ControllerBase
    {
        private readonly IGeminiService _geminiService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MailController> _logger;

        public MailController(
            IGeminiService geminiService,
            IEmailService emailService,
            IWebHostEnvironment env,
            ILogger<MailController> logger)
        {
            _geminiService = geminiService;
            _emailService = emailService;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// API Call 1: Upload JD Image, parse image with Gemini API using backend resume & prompt, return HR email & draft mail.
        /// </summary>
        [HttpPost("generate")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GenerateMail([FromForm] GenerateMailRequest request)
        {
            try
            {
                if (request.Image == null || request.Image.Length == 0)
                {
                    return BadRequest(new GenerateMailResponse
                    {
                        Success = false,
                        Message = "Please upload a valid Job Description (JD) image."
                    });
                }

                // Locate resume and prompt files in backend Data folder
                string dataFolder = Path.Combine(_env.ContentRootPath, "Data");
                string resumePath = Path.Combine(dataFolder, "resume.txt");
                string promptPath = Path.Combine(dataFolder, "prompt.txt");

                string resumeText = System.IO.File.Exists(resumePath)
                    ? await System.IO.File.ReadAllTextAsync(resumePath)
                    : "Experienced .NET Developer skilled in C#, ASP.NET Core Web API, SQL Server, and REST Services.";

                string promptText = System.IO.File.Exists(promptPath)
                    ? await System.IO.File.ReadAllTextAsync(promptPath)
                    : "Extract HR email from JD image and write a cover letter using candidate resume.";

                _logger.LogInformation("Processing JD image with Gemini API...");
                GeminiDraftResult draftResult = await _geminiService.GenerateMailDraftAsync(request.Image, resumeText, promptText);

                string resumeFileName = System.IO.File.Exists(resumePath)
                    ? Path.GetFileName(resumePath)
                    : "resume.txt";

                return Ok(new GenerateMailResponse
                {
                    Success = true,
                    Message = "Job description processed successfully with AI.",
                    HrEmail = draftResult.HrEmail,
                    Subject = draftResult.Subject,
                    Body = draftResult.Body,
                    ResumeFileName = resumeFileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate mail draft.");
                return StatusCode(500, new GenerateMailResponse
                {
                    Success = false,
                    Message = $"Generation error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// API Call 2: Send the final email message + resume attachment from backend to HR email.
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendMail([FromBody] SendMailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.HrEmail))
                {
                    return BadRequest(new SendMailResponse
                    {
                        Success = false,
                        Message = "HR Email address is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
                {
                    return BadRequest(new SendMailResponse
                    {
                        Success = false,
                        Message = "Email Subject and Body are required."
                    });
                }

                string dataFolder = Path.Combine(_env.ContentRootPath, "Data");
                string resumePath = Path.Combine(dataFolder, "Abhishek_Resume.pdf");

                if (!System.IO.File.Exists(resumePath))
                {
                    var files = Directory.GetFiles(dataFolder, "resume.*");
                    if (files.Length > 0)
                    {
                        resumePath = files[0];
                    }
                }

                _logger.LogInformation("Sending mail to HR: {HrEmail}...", request.HrEmail);
                await _emailService.SendEmailWithAttachmentAsync(
                    request.HrEmail,
                    request.Subject,
                    request.Body,
                    resumePath,
                    request.SenderEmail,
                    request.AppPassword);

                return Ok(new SendMailResponse
                {
                    Success = true,
                    Message = $"Email with resume successfully sent to {request.HrEmail}!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email.");
                return StatusCode(500, new SendMailResponse
                {
                    Success = false,
                    Message = $"{ex.Message}"
                });
            }
        }
    }
}
