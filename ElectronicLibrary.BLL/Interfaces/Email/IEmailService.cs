namespace ElectronicLibrary.BLL.Interfaces.Email;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string recipientEmail, string fullName,
        string userId,
        string confirmationToken);
    Task SendPasswordResetAsync(string recipientEmail, string fullName,
        string userId,
        string resetToken);
}
