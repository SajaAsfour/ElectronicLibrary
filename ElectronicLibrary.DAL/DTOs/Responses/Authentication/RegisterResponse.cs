namespace ElectronicLibrary.DAL.DTOs.Responses.Authentication;

public class RegisterResponse
{
    public string UserId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool EmailConfirmationRequired { get; set; }

    public string Message { get; set; } = null!;
}
