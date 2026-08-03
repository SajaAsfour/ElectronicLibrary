using System.Net;
using ElectronicLibrary.IntegrationTests.Infrastructure;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class UserManagementAuthorizationTests
{
    private readonly HttpClient _client;

    public UserManagementAuthorizationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_WithoutToken_ReturnsUnauthorized()
    {
        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/admin/users?pageNumber=1&pageSize=10");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}