public class MailRequest
{
    public string AccessToken { get; set; } = "";

    public IFormFile? Image { get; set; }
}