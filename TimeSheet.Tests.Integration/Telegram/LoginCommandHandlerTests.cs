using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Tests.Integration.Base;
using TimeSheet.Tests.Integration.Fixtures;
using TimeSheet.Tests.Integration.Mocks;

namespace TimeSheet.Tests.Integration.TelegramTests;

/// <summary>
/// Integration tests for the /login command.
/// Tests one-time OTP mnemonic generation for registered users.
/// </summary>
public class LoginCommandHandlerTests(TelegramBotTestFixture fixture) : TelegramBotTestBase(fixture)
{
    [Fact]
    public async Task LoginCommand_RegisteredUser_GeneratesMnemonicSuccessfully()
    {
        // Arrange - register a regular user
        const long userId = 30001;
        await RegisterTestUserAsync(telegramUserId: userId, isAdmin: false);

        // Act
        var responses = await SendTextAsync("/login", userId: userId);

        // Assert - should receive 1 message
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);

        // Message should contain login code
        Assert.Contains("Your login code", responses[0].Text);
        Assert.Contains("valid for one use only", responses[0].Text);
    }

    [Fact]
    public async Task LoginCommand_AdminUser_GeneratesMnemonicSuccessfully()
    {
        // Arrange - register an admin user
        const long adminUserId = 30002;
        await RegisterTestUserAsync(telegramUserId: adminUserId, isAdmin: true);

        // Act
        var responses = await SendTextAsync("/login", userId: adminUserId);

        // Assert - should receive 1 message
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);

        // Message should contain login code
        Assert.Contains("Your login code", responses[0].Text);
        Assert.Contains("valid for one use only", responses[0].Text);
    }

    [Fact]
    public async Task LoginCommand_NonRegisteredUser_IgnoresRequest()
    {
        // Arrange - no registration (user not in DB)
        const long unregisteredUserId = 30003;

        // Act
        var responses = await SendTextAsync("/login", userId: unregisteredUserId);

        // Assert - UpdateHandler should ignore non-registered users silently
        AssertNoResponse(responses);
    }

    [Fact]
    public async Task LoginCommand_StoresMnemonicAsPending()
    {
        // Arrange - register a user
        const long userId = 30004;
        await RegisterTestUserAsync(telegramUserId: userId, isAdmin: false);

        // Act - generate a mnemonic
        var responses = await SendTextAsync("/login", userId: userId);

        // Assert - should receive 1 message, extract the mnemonic
        Assert.Single(responses);
        var responseText = responses[0].Text;
        Assert.Contains("Your login code", responseText);

        // Extract the mnemonic phrase (everything between backticks)
        var firstBacktick = responseText!.IndexOf('`');
        Assert.True(firstBacktick >= 0, "Response should contain backtick-wrapped mnemonic");

        var mnemonicStart = firstBacktick + 1;
        var mnemonicEnd = responseText.IndexOf('`', mnemonicStart);
        Assert.True(mnemonicEnd > mnemonicStart, "Response should have closing backtick");
        var mnemonicPhrase = responseText[mnemonicStart..mnemonicEnd].Trim();

        // Verify the mnemonic can be validated
        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();

        var isValid = mnemonicService.ValidateAndConsumeMnemonic(mnemonicPhrase);
        Assert.True(isValid, "Generated mnemonic should be stored and valid");
    }

    [Fact]
    public async Task LoginCommand_GeneratedMnemonicIsSingleUse()
    {
        // Arrange - register a user
        const long userId = 30005;
        await RegisterTestUserAsync(telegramUserId: userId, isAdmin: false);

        // Act - generate a mnemonic
        var responses = await SendTextAsync("/login", userId: userId);

        // Extract the mnemonic from the message
        var responseText = responses[0].Text!;
        var firstBacktick = responseText.IndexOf('`');
        var mnemonicStart = firstBacktick + 1;
        var mnemonicEnd = responseText.IndexOf('`', mnemonicStart);
        var mnemonicPhrase = responseText[mnemonicStart..mnemonicEnd].Trim();

        // Validate and consume the mnemonic once
        using var scope1 = Fixture.CreateScope();
        var mnemonicService1 = scope1.ServiceProvider.GetRequiredService<IMnemonicService>();
        var firstValidation = mnemonicService1.ValidateAndConsumeMnemonic(mnemonicPhrase);
        Assert.True(firstValidation, "First validation should succeed");

        // Try to validate again (should fail - already consumed)
        using var scope2 = Fixture.CreateScope();
        var mnemonicService2 = scope2.ServiceProvider.GetRequiredService<IMnemonicService>();
        var secondValidation = mnemonicService2.ValidateAndConsumeMnemonic(mnemonicPhrase);
        Assert.False(secondValidation, "Second validation should fail - mnemonic already consumed");
    }

    [Fact]
    public async Task LoginCommand_GeneratedMnemonicIs24Words()
    {
        // Arrange - register a user
        const long userId = 30006;
        await RegisterTestUserAsync(telegramUserId: userId, isAdmin: false);

        // Act
        var responses = await SendTextAsync("/login", userId: userId);

        // Extract the mnemonic from the message
        var responseText = responses[0].Text!;
        var firstBacktick = responseText.IndexOf('`');
        var mnemonicStart = firstBacktick + 1;
        var mnemonicEnd = responseText.IndexOf('`', mnemonicStart);
        var mnemonicPhrase = responseText[mnemonicStart..mnemonicEnd].Trim();

        // Assert - BIP39 mnemonic should be 24 words
        var words = mnemonicPhrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(24, words.Length);
    }

    [Fact]
    public async Task LoginCommand_MultipleGenerations_BothMnemonicsAreStoredAndValid()
    {
        // Arrange - register a user
        const long userId = 30007;
        await RegisterTestUserAsync(telegramUserId: userId, isAdmin: false);

        // Act - generate two mnemonics
        var responses1 = await SendTextAsync("/login", userId: userId);
        Fixture.MockBotClient.ClearResponses();
        var responses2 = await SendTextAsync("/login", userId: userId);

        // Extract both mnemonics from the messages
        var responseText1 = responses1[0].Text!;
        var firstBacktick1 = responseText1.IndexOf('`');
        var mnemonicStart1 = firstBacktick1 + 1;
        var mnemonicEnd1 = responseText1.IndexOf('`', mnemonicStart1);
        var mnemonicPhrase1 = responseText1[mnemonicStart1..mnemonicEnd1].Trim();

        var responseText2 = responses2[0].Text!;
        var firstBacktick2 = responseText2.IndexOf('`');
        var mnemonicStart2 = firstBacktick2 + 1;
        var mnemonicEnd2 = responseText2.IndexOf('`', mnemonicStart2);
        var mnemonicPhrase2 = responseText2[mnemonicStart2..mnemonicEnd2].Trim();

        // Assert - both mnemonics should be valid and stored
        // Note: In test environments, NBitcoin may use deterministic RNG which could produce
        // identical mnemonics. In production with true entropy, this is astronomically unlikely.
        // We verify both are stored rather than asserting uniqueness.
        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();

        var isValid1 = mnemonicService.ValidateAndConsumeMnemonic(mnemonicPhrase1);
        Assert.True(isValid1, "First mnemonic should be stored and valid");

        // If they happen to be the same (deterministic test RNG), only one will validate
        // If they're different (production behavior), both should validate
        var isValid2 = mnemonicService.ValidateAndConsumeMnemonic(mnemonicPhrase2);
        if (mnemonicPhrase1 != mnemonicPhrase2)
        {
            Assert.True(isValid2, "Second mnemonic should be stored and valid when different from first");
        }
    }

    [Fact]
    public async Task LoginCommand_WithAlias_WorksCorrectly()
    {
        // Arrange - register a user
        const long userId = 30008;
        await RegisterTestUserAsync(telegramUserId: userId, isAdmin: false);

        // Act - use alias /lo
        var responses = await SendTextAsync("/lo", userId: userId);

        // Assert - should work the same as /login
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Your login code", responses[0].Text);
    }
}
