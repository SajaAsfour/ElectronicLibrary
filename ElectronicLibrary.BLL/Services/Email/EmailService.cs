using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.BLL.Interfaces.Email;
using ElectronicLibrary.BLL.Options;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace ElectronicLibrary.BLL.Services.Email;

public class EmailService : IEmailService
{
    private readonly EmailOptions _emailOptions;

    public EmailService(IOptions<EmailOptions> emailOptions)
    {
        _emailOptions = emailOptions.Value;
    }

    public Task SendEmailConfirmationAsync(
        string recipientEmail,
        string fullName,
        string userId,
        string confirmationToken)
    {
        var confirmationUrl = BuildActionUrl(
            _emailOptions.ConfirmationUrl,
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

        return SendAsync(
            recipientEmail,
            "Confirm your Electronic Library account",
            body);
    }

    public Task SendPasswordResetAsync(
        string recipientEmail,
        string fullName,
        string userId,
        string resetToken)
    {
        var resetUrl = BuildActionUrl(
            _emailOptions.PasswordResetUrl,
            userId,
            resetToken);

        var safeFullName = WebUtility.HtmlEncode(fullName);
        var safeResetUrl = WebUtility.HtmlEncode(resetUrl);

        var body = $"""
            <h2>Reset your password</h2>
            <p>Hello {safeFullName},</p>
            <p>We received a request to reset your password.</p>
            <p>
                <a href="{safeResetUrl}">Reset Password</a>
            </p>
            <p>This link expires according to the configured Identity token lifetime.</p>
            <p>If you did not request a password reset, you can ignore this email.</p>
            """;

        return SendAsync(
            recipientEmail,
            "Reset your Electronic Library password",
            body);
    }

    private async Task SendAsync(
        string recipientEmail,
        string subject,
        string body)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(
                    _emailOptions.FromEmail,
                    _emailOptions.FromName),
                Subject = subject,
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

    private static string BuildActionUrl(string baseUrl, string userId, string token)
    {
        var separator = baseUrl.Contains('?') ? "&" : "?";

        var urlSafeToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(token));

        return
            $"{baseUrl}{separator}" +
            $"userId={Uri.EscapeDataString(userId)}&" +
            $"token={urlSafeToken}";
    }
}
