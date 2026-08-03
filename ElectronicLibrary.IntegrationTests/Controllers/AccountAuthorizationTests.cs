using System.Net;
using System.Net.Http.Json;
using ElectronicLibrary.IntegrationTests.Infrastructure;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class AccountAuthorizationTests
{
    private readonly HttpClient _client;

    public AccountAuthorizationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        HttpResponseMessage response =
            await _client.GetAsync("/api/account/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateSellerProfile_WithoutToken_ReturnsUnauthorized()
    {
        var request = new
        {
            storeName = "Test Store",
            bio = "Integration test store"
        };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/account/seller-profile",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var request = new
        {
            email = "missing-user@example.com",
            password = "WrongPassword@123"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/account/login",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}