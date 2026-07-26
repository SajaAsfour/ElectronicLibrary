using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.BLL.Interfaces.Email;
using ElectronicLibrary.BLL.Options;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace ElectronicLibrary.BLL.Services.Email;

public class EmailService : IEmailService
{
    private readonly EmailOptions _emailOptions;

    public EmailService(IOptions<EmailOptions> emailOptions)
    {
        _emailOptions = emailOptions.Value;
    }

    public async Task SendEmailConfirmationAsync(
        string recipientEmail,
        string fullName,
        string userId,
        string confirmationToken)
    {
        try
        {
            var confirmationUrl = BuildConfirmationUrl(
                userId,
                confirmationToken);

            var safeFullName = WebUtility.HtmlEncode(fullName);
            var safeConfirmationUrl =
                WebUtility.HtmlEncode(confirmationUrl);

            var body = $"""
                <h2>Welcome to Electronic Library Marketplace</h2>
                <p>Hello {safeFullName},</p>
                <p>Please confirm your email address to activate your account.</p>
                <p>
                    <a href="{safeConfirmationUrl}">Confirm Email</a>
                </p>
                <p>If you did not create this account, you can ignore this email.</p>
                """;

            using var message = new MailMessage
            {
                From = new MailAddress(
                    _emailOptions.FromEmail,
                    _emailOptions.FromName),
                Subject = "Confirm your Electronic Library account",
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(recipientEmail);

            using var smtpClient = new SmtpClient(
                _emailOptions.Host,
                _emailOptions.Port)
            {
                EnableSsl = _emailOptions.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    _emailOptions.UserName,
                    _emailOptions.Password)
            };

            await smtpClient.SendMailAsync(message);
        }
        catch (Exception exception)
            when (exception is SmtpException
                  or InvalidOperationException
                  or FormatException)
        {
            throw new EmailDeliveryException(
                "EmailDeliveryFailed",
                exception);
        }
    }

    private string BuildConfirmationUrl(
        string userId,
        string confirmationToken)
    {
        var separator = _emailOptions.ConfirmationUrl.Contains('?')
            ? "&"
            : "?";

        return
            $"{_emailOptions.ConfirmationUrl}{separator}" +
            $"userId={Uri.EscapeDataString(userId)}&" +
            $"token={Uri.EscapeDataString(confirmationToken)}";
    }
}
