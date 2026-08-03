using ElectronicLibrary.BLL.Interfaces.Common;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ElectronicLibrary.BLL.Services.Common;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetUserId()
    {
        var userId =
            _httpContextAccessor.HttpContext?
                .User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "UnauthorizedRequest");
        }

        return userId;
    }
}