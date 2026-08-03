using ElectronicLibrary.BLL.Interfaces.Common;

namespace ElectronicLibrary.UnitTests.Helpers;

public sealed class FakeCurrentUserService
    : ICurrentUserService
{
    public FakeCurrentUserService(
        string userId = "unit-test-admin-id")
    {
        UserId = userId;
    }

    public string UserId { get; set; }

    public string GetUserId()
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            throw new UnauthorizedAccessException(
                "UnauthorizedRequest");
        }

        return UserId;
    }
}