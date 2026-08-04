using ElectronicLibrary.BLL.Interfaces.Authentication;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.DTOs.Requests.Sellers;
using ElectronicLibrary.DAL.DTOs.Responses.Sellers;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class SellersControllerTests
{
    private const string TestPassword =
        "SellerIntegrationTest@123";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SellersControllerTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task
        ActivateSeller_WithoutToken_ReturnsUnauthorized()
    {
        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        var request = new ActivateSellerRequest
        {
            StoreName = CreateUniqueStoreName(),
            SellerBio = "Integration test seller."
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/sellers/activate",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task
        ActivateSeller_AsCustomer_ReturnsOkAndAddsSellerRole()
    {
        TestUserContext customer =
            await CreateAuthenticatedUserAsync(
                ApplicationRoles.Customer);

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                customer.AccessToken);

        string storeName =
            CreateUniqueStoreName();

        var request = new ActivateSellerRequest
        {
            StoreName = $"  {storeName}  ",
            SellerBio =
                "  Books for integration tests.  "
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/sellers/activate",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        SellerProfileResponse? result =
            await response.Content
                .ReadFromJsonAsync<
                    SellerProfileResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            customer.UserId,
            result.UserId);

        Assert.Equal(
            storeName,
            result.StoreName);

        Assert.Equal(
            "Books for integration tests.",
            result.SellerBio);

        Assert.True(result.IsSeller);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        ApplicationUser? savedUser =
            await userManager.FindByIdAsync(
                customer.UserId);

        Assert.NotNull(savedUser);

        Assert.Equal(
            storeName,
            savedUser.StoreName);

        Assert.Equal(
            "Books for integration tests.",
            savedUser.SellerBio);

        Assert.True(
            await userManager.IsInRoleAsync(
                savedUser,
                ApplicationRoles.Seller));
    }

    [Fact]
    public async Task
        ActivateSeller_WithDuplicateStoreName_ReturnsConflict()
    {
        string storeName =
            CreateUniqueStoreName();

        await CreateAuthenticatedUserAsync(
            ApplicationRoles.Seller,
            storeName: storeName);

        TestUserContext customer =
            await CreateAuthenticatedUserAsync(
                ApplicationRoles.Customer);

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                customer.AccessToken);

        var request = new ActivateSellerRequest
        {
            StoreName =
                storeName.ToLowerInvariant()
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/sellers/activate",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task
        ActivateSeller_WhenAlreadySeller_ReturnsConflict()
    {
        TestUserContext seller =
            await CreateAuthenticatedUserAsync(
                ApplicationRoles.Seller,
                storeName: CreateUniqueStoreName());

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                seller.AccessToken);

        var request = new ActivateSellerRequest
        {
            StoreName = CreateUniqueStoreName()
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/sellers/activate",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task
        GetCurrentSellerProfile_AsCustomer_ReturnsForbidden()
    {
        TestUserContext customer =
            await CreateAuthenticatedUserAsync(
                ApplicationRoles.Customer);

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/sellers/me");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task
        GetCurrentSellerProfile_AsSeller_ReturnsProfile()
    {
        string storeName =
            CreateUniqueStoreName();

        TestUserContext seller =
            await CreateAuthenticatedUserAsync(
                ApplicationRoles.Seller,
                storeName: storeName,
                sellerBio: "Seller biography",
                sellerRating: 4.50m,
                city: "Ramallah",
                address: "Main Street");

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                seller.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/sellers/me");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        SellerProfileResponse? result =
            await response.Content
                .ReadFromJsonAsync<
                    SellerProfileResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            seller.UserId,
            result.UserId);

        Assert.Equal(
            storeName,
            result.StoreName);

        Assert.Equal(
            "Seller biography",
            result.SellerBio);

        Assert.Equal(
            4.50m,
            result.SellerRating);

        Assert.Equal(
            "Ramallah",
            result.City);

        Assert.Equal(
            "Main Street",
            result.Address);

        Assert.True(result.IsSeller);
    }

    [Fact]
    public async Task
        UpdateCurrentSellerProfile_AsSeller_UpdatesProfile()
    {
        TestUserContext seller =
            await CreateAuthenticatedUserAsync(
                ApplicationRoles.Seller,
                storeName: CreateUniqueStoreName(),
                sellerRating: 4.25m);

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                seller.AccessToken);

        string updatedStoreName =
            CreateUniqueStoreName();

        var request =
            new UpdateSellerProfileRequest
            {
                StoreName =
                    $"  {updatedStoreName}  ",
                SellerBio =
                    "  Updated seller biography.  ",
                City = "  Nablus  ",
                Address = "  City Center  "
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/sellers/me",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        SellerProfileResponse? result =
            await response.Content
                .ReadFromJsonAsync<
                    SellerProfileResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            updatedStoreName,
            result.StoreName);

        Assert.Equal(
            "Updated seller biography.",
            result.SellerBio);

        Assert.Equal(
            "Nablus",
            result.City);

        Assert.Equal(
            "City Center",
            result.Address);

        Assert.Equal(
            4.25m,
            result.SellerRating);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        ApplicationUser? savedUser =
            await userManager.FindByIdAsync(
                seller.UserId);

        Assert.NotNull(savedUser);

        Assert.Equal(
            updatedStoreName,
            savedUser.StoreName);

        Assert.Equal(
            "Updated seller biography.",
            savedUser.SellerBio);

        Assert.Equal(
            "Nablus",
            savedUser.City);

        Assert.Equal(
            "City Center",
            savedUser.Address);

        Assert.Equal(
            4.25m,
            savedUser.SellerRating);
    }

    [Fact]
    public async Task
        UpdateCurrentSellerProfile_WithDuplicateStoreName_ReturnsConflict()
    {
        string existingStoreName =
            CreateUniqueStoreName();

        await CreateAuthenticatedUserAsync(
            ApplicationRoles.Seller,
            storeName: existingStoreName);

        TestUserContext currentSeller =
            await CreateAuthenticatedUserAsync(
                ApplicationRoles.Seller,
                storeName: CreateUniqueStoreName());

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                currentSeller.AccessToken);

        var request =
            new UpdateSellerProfileRequest
            {
                StoreName =
                    existingStoreName
                        .ToLowerInvariant()
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/sellers/me",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task
        GetPublicSellerProfile_WithoutToken_ReturnsPublicData()
    {
        TestUserContext seller =
            await CreateAuthenticatedUserAsync(
                ApplicationRoles.Seller,
                storeName: CreateUniqueStoreName(),
                sellerBio: "Public seller biography",
                sellerRating: 4.75m,
                city: "Hebron",
                address: "Private Address");

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                $"/api/sellers/{seller.UserId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        PublicSellerProfileResponse? result =
            await response.Content
                .ReadFromJsonAsync<
                    PublicSellerProfileResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            seller.UserId,
            result.UserId);

        Assert.Equal(
            "Public seller biography",
            result.SellerBio);

        Assert.Equal(
            4.75m,
            result.SellerRating);

        Assert.Equal(
            "Hebron",
            result.City);

        string json =
            await response.Content
                .ReadAsStringAsync();

        using JsonDocument document =
            JsonDocument.Parse(json);

        JsonElement root =
            document.RootElement;

        Assert.False(
            root.TryGetProperty(
                "email",
                out _));

        Assert.False(
            root.TryGetProperty(
                "address",
                out _));

        Assert.False(
            root.TryGetProperty(
                "phoneNumber",
                out _));

        Assert.False(
            root.TryGetProperty(
                "refreshTokenHash",
                out _));
    }

    [Fact]
    public async Task
        GetPublicSellerProfile_WithNonSeller_ReturnsNotFound()
    {
        TestUserContext customer =
            await CreateAuthenticatedUserAsync(
                ApplicationRoles.Customer);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                $"/api/sellers/{customer.UserId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private async Task<TestUserContext>
        CreateAuthenticatedUserAsync(
            string role,
            string? storeName = null,
            string? sellerBio = null,
            decimal? sellerRating = null,
            string? city = null,
            string? address = null)
    {
        using IServiceScope scope =
            _factory.Services.CreateScope();

        IServiceProvider services =
            scope.ServiceProvider;

        RoleManager<IdentityRole> roleManager =
            services.GetRequiredService<
                RoleManager<IdentityRole>>();

        UserManager<ApplicationUser> userManager =
            services.GetRequiredService<
                UserManager<ApplicationUser>>();

        ITokenService tokenService =
            services.GetRequiredService<
                ITokenService>();

        if (!await roleManager
                .RoleExistsAsync(role))
        {
            IdentityResult roleResult =
                await roleManager.CreateAsync(
                    new IdentityRole(role));

            EnsureIdentityResultSucceeded(
                roleResult);
        }

        string suffix =
            Guid.NewGuid().ToString("N");

        string email =
            $"{role.ToLowerInvariant()}-" +
            $"{suffix}@integrationtests.local";

        var user = new ApplicationUser
        {
            FullName =
                $"{role} Integration User",
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            StoreName = storeName,
            SellerBio = sellerBio,
            SellerRating = sellerRating,
            City = city,
            Address = address
        };

        IdentityResult createResult =
            await userManager.CreateAsync(
                user,
                TestPassword);

        EnsureIdentityResultSucceeded(
            createResult);

        IdentityResult roleAssignmentResult =
            await userManager.AddToRoleAsync(
                user,
                role);

        EnsureIdentityResultSucceeded(
            roleAssignmentResult);

        string accessToken =
            await tokenService
                .CreateAccessTokenAsync(user);

        return new TestUserContext(
            user.Id,
            accessToken);
    }

    private static string CreateUniqueStoreName()
    {
        return $"Store-{Guid.NewGuid():N}";
    }

    private static void
        EnsureIdentityResultSucceeded(
            IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        string errors = string.Join(
            Environment.NewLine,
            result.Errors.Select(
                error =>
                    $"{error.Code}: " +
                    error.Description));

        throw new InvalidOperationException(
            errors);
    }

    private sealed record TestUserContext(
        string UserId,
        string AccessToken);
}
