using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using DOJO2.Application.Common;

namespace DOJO2.Infrastructure.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger _logger;

        public EmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor,
            ILogger<EmailSender> logger)
        {
            Options = optionsAccessor.Value;
            _logger = logger;
        }

        public AuthMessageSenderOptions Options { get; } //Set with Secret Manager.

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrEmpty(Options.SendGridKey))
            {
                throw new ArgumentException("SendGridKey не налаштований");
            }
            await Execute(Options.SendGridKey, subject, htmlMessage, email);
        }

        public async Task<Result> SendEmailWithResultAsync(string toEmail, string subject, string message)
        {
            if (string.IsNullOrEmpty(toEmail))
            {
                return Result.FailureResult("Email не може бути порожнім");
            }

            if (string.IsNullOrEmpty(Options.SendGridKey))
            {
                _logger.LogError("SendGridKey не налаштований");
                return Result.FailureResult("Сервіс email недоступний");
            }

            var client = new SendGridClient(Options.SendGridKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("kahnovets.ap@gmail.com", "Password Recovery"),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(toEmail));

            msg.SetClickTracking(false, false);
            var response = await client.SendEmailAsync(msg);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email успішно відправлено на {Email}", toEmail);
                return Result.SuccessResult($"Email відправлено на {toEmail}");
            }

            _logger.LogError("Помилка при відправці email на {Email}", toEmail);
            return Result.FailureResult("Не вдалося відправити email");
        }

        private async Task Execute(string apiKey, string subject, string message, string toEmail)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("kahnovets.ap@gmail.com", "Password Recovery"),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(toEmail));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);
            var response = await client.SendEmailAsync(msg);
            _logger.LogInformation(response.IsSuccessStatusCode
                ? "Email to {Email} queued successfully!"
                : "Failure Email to {Email}", toEmail);
        }
    }

    public class AuthMessageSenderOptions
    {
        public string? SendGridKey { get; set; }
    }
}