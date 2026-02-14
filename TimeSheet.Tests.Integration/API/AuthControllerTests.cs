using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Infrastructure.Persistence;
using TimeSheet.Presentation.API.Models.Auth;
using TimeSheet.Tests.Integration.Fixtures;

namespace TimeSheet.Tests.Integration.API;

/// <summary>
/// Integration tests for AuthController endpoints.
/// Tests login and token refresh functionality.
/// </summary>
public class AuthControllerTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public AuthControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
    }

    [Fact(Skip = "EF Core provider conflict - Sqlite + InMemory cannot coexist in same service provider")]
    public async Task Login_WithValidMnemonic_ReturnsToken()
    {
        // Arrange
        _fixture.ClearDatabase();

        // Register a test user
        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User(
                telegramUserId: 12345,
                telegramUsername: "testuser",
                isAdmin: true,
                utcOffsetMinutes: 0);

            dbContext.Set<User>().Add(user);
            await dbContext.SaveChangesAsync();
        }

        // Generate and store a pending mnemonic
        string mnemonicString;
        using (var scope = _fixture.Services.CreateScope())
        {
            var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
            var mnemonic = mnemonicService.GenerateMnemonic();
            mnemonicService.StorePendingMnemonic(mnemonic);
            mnemonicString = mnemonic.ToString();
        }

        var loginRequest = new LoginRequest
        {
            Mnemonic = mnemonicString
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.NotNull(loginResponse.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.AccessToken));
        Assert.True(loginResponse.ExpiresAt > DateTimeOffset.UtcNow);

        // Verify mnemonic was consumed
        using (var scope = _fixture.Services.CreateScope())
        {
            var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
            var isStillValid = mnemonicService.ValidateMnemonic(mnemonicString);
            Assert.False(isStillValid);
        }
    }

    [Fact(Skip = "EF Core provider conflict - Sqlite + InMemory cannot coexist in same service provider")]
    public async Task Login_WithInvalidMnemonic_ReturnsUnauthorized()
    {
        // Arrange
        _fixture.ClearDatabase();

        var loginRequest = new LoginRequest
        {
            Mnemonic = "invalid mnemonic that does not exist"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(Skip = "EF Core provider conflict - Sqlite + InMemory cannot coexist in same service provider")]
    public async Task Login_WithEmptyMnemonic_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Mnemonic = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Skip = "EF Core provider conflict - Sqlite + InMemory cannot coexist in same service provider")]
    public async Task Login_WithReusedMnemonic_ReturnsUnauthorized()
    {
        // Arrange
        _fixture.ClearDatabase();

        // Register a test user
        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User(
                telegramUserId: 12345,
                telegramUsername: "testuser",
                isAdmin: true,
                utcOffsetMinutes: 0);

            dbContext.Set<User>().Add(user);
            await dbContext.SaveChangesAsync();
        }

        // Generate and store a pending mnemonic
        string mnemonicString;
        using (var scope = _fixture.Services.CreateScope())
        {
            var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
            var mnemonic = mnemonicService.GenerateMnemonic();
            mnemonicService.StorePendingMnemonic(mnemonic);
            mnemonicString = mnemonic.ToString();
        }

        var loginRequest = new LoginRequest
        {
            Mnemonic = mnemonicString
        };

        // Act - First login (should succeed)
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act - Second login with same mnemonic (should fail)
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
    }

    [Fact(Skip = "EF Core provider conflict - Sqlite + InMemory cannot coexist in same service provider")]
    public async Task RefreshToken_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        _fixture.ClearDatabase();

        // Register a test user and login to get a token
        string accessToken;
        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User(
                telegramUserId: 12345,
                telegramUsername: "testuser",
                isAdmin: true,
                utcOffsetMinutes: 0);

            dbContext.Set<User>().Add(user);
            await dbContext.SaveChangesAsync();

            // Generate token
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            accessToken = jwtService.GenerateToken(user.TelegramUserId, user.TelegramUsername, user.IsAdmin);
        }

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = accessToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refreshResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(refreshResponse);
        Assert.NotNull(refreshResponse.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(refreshResponse.AccessToken));
        Assert.True(refreshResponse.ExpiresAt > DateTimeOffset.UtcNow);

        // New token should be different from the old one
        Assert.NotEqual(accessToken, refreshResponse.AccessToken);
    }

    [Fact(Skip = "EF Core provider conflict - Sqlite + InMemory cannot coexist in same service provider")]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid.token.here"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(Skip = "EF Core provider conflict - Sqlite + InMemory cannot coexist in same service provider")]
    public async Task RefreshToken_WithEmptyToken_ReturnsBadRequest()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Skip = "EF Core provider conflict - Sqlite + InMemory cannot coexist in same service provider")]
    public async Task RefreshToken_ForDeletedUser_ReturnsUnauthorized()
    {
        // Arrange
        _fixture.ClearDatabase();

        // Register a test user and get a token
        string accessToken;
        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User(
                telegramUserId: 12345,
                telegramUsername: "testuser",
                isAdmin: true,
                utcOffsetMinutes: 0);

            dbContext.Set<User>().Add(user);
            await dbContext.SaveChangesAsync();

            // Generate token
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            accessToken = jwtService.GenerateToken(user.TelegramUserId, user.TelegramUsername, user.IsAdmin);
        }

        // Delete the user
        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await dbContext.Set<User>().FirstOrDefaultAsync(u => u.TelegramUserId == 12345);
            if (user != null)
            {
                dbContext.Set<User>().Remove(user);
                await dbContext.SaveChangesAsync();
            }
        }

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = accessToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
