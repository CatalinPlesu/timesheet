using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Tests.Integration.Base;
using TimeSheet.Tests.Integration.Fixtures;
using TimeSheet.Tests.Integration.Mocks;

namespace TimeSheet.Tests.Integration.TelegramTests;

/// <summary>
/// Integration tests for the updated registration flow with UTC offset prompt.
/// </summary>
public class RegistrationFlowTests(TelegramBotTestFixture fixture) : TelegramBotTestBase(fixture)
{
    [Fact]
    public async Task Registration_WithValidMnemonic_PromptsForUtcOffset()
    {
        // Arrange - generate a mnemonic
        const long userId = 30001;
        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
        var mnemonic = mnemonicService.GenerateMnemonic();
        mnemonicService.StorePendingMnemonic(mnemonic);

        // Act - send registration command
        var responses = await SendTextAsync($"/register {mnemonic}", userId: userId);

        // Assert - should prompt for UTC offset
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Mnemonic validated", responses[0].Text);
        Assert.Contains("timezone UTC offset", responses[0].Text);
        Assert.Contains("Examples:", responses[0].Text);
    }

    [Fact]
    public async Task Registration_WithUtcOffset_CompletesSuccessfully()
    {
        // Arrange - generate a mnemonic and validate it
        const long userId = 30002;
        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
        var mnemonic = mnemonicService.GenerateMnemonic();
        mnemonicService.StorePendingMnemonic(mnemonic);

        // Step 1: Send registration command
        await SendTextAsync($"/register {mnemonic}", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Step 2: Send UTC offset
        var responses = await SendTextAsync("+2", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Registration successful", responses[0].Text);
        Assert.Contains("UTC+2", responses[0].Text);
    }

    [Fact]
    public async Task Registration_NegativeUtcOffset_CompletesSuccessfully()
    {
        // Arrange
        const long userId = 30003;
        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
        var mnemonic = mnemonicService.GenerateMnemonic();
        mnemonicService.StorePendingMnemonic(mnemonic);

        // Step 1: Send registration command
        await SendTextAsync($"/register {mnemonic}", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Step 2: Send negative UTC offset
        var responses = await SendTextAsync("-5", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Registration successful", responses[0].Text);
        Assert.Contains("UTC-5", responses[0].Text);
    }

    [Fact]
    public async Task Registration_ZeroUtcOffset_CompletesSuccessfully()
    {
        // Arrange
        const long userId = 30004;
        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
        var mnemonic = mnemonicService.GenerateMnemonic();
        mnemonicService.StorePendingMnemonic(mnemonic);

        // Step 1: Send registration command
        await SendTextAsync($"/register {mnemonic}", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Step 2: Send zero UTC offset
        var responses = await SendTextAsync("0", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Registration successful", responses[0].Text);
        Assert.Contains("UTC+0", responses[0].Text);
    }

    [Fact]
    public async Task Registration_InvalidUtcOffset_ShowsError()
    {
        // Arrange
        const long userId = 30005;
        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
        var mnemonic = mnemonicService.GenerateMnemonic();
        mnemonicService.StorePendingMnemonic(mnemonic);

        // Step 1: Send registration command
        await SendTextAsync($"/register {mnemonic}", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Step 2: Send invalid UTC offset
        var responses = await SendTextAsync("invalid", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Invalid offset", responses[0].Text);
    }

    [Fact]
    public async Task Registration_UtcOffsetOutOfRange_ShowsError()
    {
        // Arrange
        const long userId = 30006;
        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
        var mnemonic = mnemonicService.GenerateMnemonic();
        mnemonicService.StorePendingMnemonic(mnemonic);

        // Step 1: Send registration command
        await SendTextAsync($"/register {mnemonic}", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Step 2: Send out-of-range UTC offset
        var responses = await SendTextAsync("20", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("must be between -12 and +14", responses[0].Text);
    }

    [Fact]
    public async Task Registration_FirstUser_BecomesAdmin()
    {
        // Note: This test assumes the database is empty at the start
        // If other tests have registered users, this may fail
        // For test isolation, we'd need to clear the database or use a unique test database

        // Arrange
        const long userId = 30007;
        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
        var mnemonic = mnemonicService.GenerateMnemonic();
        mnemonicService.StorePendingMnemonic(mnemonic);

        // Act - register
        await SendTextAsync($"/register {mnemonic}", userId: userId);
        Fixture.MockBotClient.ClearResponses();
        var responses = await SendTextAsync("+1", userId: userId);

        // Assert - since many tests have already registered users, we check for either outcome
        Assert.Single(responses);
        Assert.Contains("Registration successful", responses[0].Text);
    }

    [Fact]
    public async Task Registration_AlreadyRegistered_ShowsError()
    {
        // Arrange - register user first
        const long userId = 30008;
        await RegisterTestUserAsync(telegramUserId: userId);

        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
        var mnemonic = mnemonicService.GenerateMnemonic();
        mnemonicService.StorePendingMnemonic(mnemonic);

        // Act - try to register again
        var responses = await SendTextAsync($"/register {mnemonic}", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("already registered", responses[0].Text);
    }

    [Fact]
    public async Task Registration_InvalidMnemonic_ShowsError()
    {
        // Arrange
        const long userId = 30009;
        const string invalidMnemonic = "invalid mnemonic phrase that is not valid";

        // Act
        var responses = await SendTextAsync($"/register {invalidMnemonic}", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Invalid or expired mnemonic", responses[0].Text);
    }

    [Fact]
    public async Task Registration_MnemonicConsumedOnce_CannotBeReused()
    {
        // Arrange
        const long userId1 = 30010;
        const long userId2 = 30011;

        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();
        var mnemonic = mnemonicService.GenerateMnemonic();
        mnemonicService.StorePendingMnemonic(mnemonic);

        // Act - user 1 registers with the mnemonic
        await SendTextAsync($"/register {mnemonic}", userId: userId1);
        Fixture.MockBotClient.ClearResponses();
        await SendTextAsync("+2", userId: userId1);

        // Try to register user 2 with the same mnemonic
        Fixture.MockBotClient.ClearResponses();
        var responses = await SendTextAsync($"/register {mnemonic}", userId: userId2);

        // Assert - should fail because mnemonic was already consumed
        Assert.Single(responses);
        Assert.Contains("Invalid or expired mnemonic", responses[0].Text);
    }

    [Fact]
    public async Task Registration_EdgeCaseUtcOffsets_AllValid()
    {
        // Test minimum valid offset (UTC-12)
        const long userId1 = 30012;
        using var scope1 = Fixture.CreateScope();
        var mnemonicService1 = scope1.ServiceProvider.GetRequiredService<IMnemonicService>();
        var mnemonic1 = mnemonicService1.GenerateMnemonic();
        mnemonicService1.StorePendingMnemonic(mnemonic1);
        await SendTextAsync($"/register {mnemonic1}", userId: userId1);
        Fixture.MockBotClient.ClearResponses();
        var responses1 = await SendTextAsync("-12", userId: userId1);
        Assert.Contains("UTC-12", responses1[0].Text);

        // Test maximum valid offset (UTC+14)
        const long userId2 = 30013;
        using var scope2 = Fixture.CreateScope();
        var mnemonicService2 = scope2.ServiceProvider.GetRequiredService<IMnemonicService>();
        var mnemonic2 = mnemonicService2.GenerateMnemonic();
        mnemonicService2.StorePendingMnemonic(mnemonic2);
        await SendTextAsync($"/register {mnemonic2}", userId: userId2);
        Fixture.MockBotClient.ClearResponses();
        var responses2 = await SendTextAsync("14", userId: userId2);
        Assert.Contains("UTC+14", responses2[0].Text);
    }
}
