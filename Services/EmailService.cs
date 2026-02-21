using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;


namespace TenderSystem.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress("Tender System", emailSettings["FromEmail"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(emailSettings["SmtpServer"],
                emailSettings.GetValue<int>("SmtpPort"),
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(emailSettings["FromEmail"], emailSettings["FromPassword"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}