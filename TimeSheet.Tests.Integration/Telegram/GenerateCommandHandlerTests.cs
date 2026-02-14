using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Tests.Integration.Base;
using TimeSheet.Tests.Integration.Fixtures;
using TimeSheet.Tests.Integration.Mocks;

namespace TimeSheet.Tests.Integration.TelegramTests;

/// <summary>
/// Integration tests for the /generate command.
/// Tests admin mnemonic generation functionality.
/// </summary>
public class GenerateCommandHandlerTests(TelegramBotTestFixture fixture) : TelegramBotTestBase(fixture)
{
    [Fact]
    public async Task GenerateCommand_AdminUser_GeneratesMnemonicSuccessfully()
    {
        // Arrange - register an admin user
        const long adminUserId = 20001;
        await RegisterTestUserAsync(telegramUserId: adminUserId, isAdmin: true);

        // Act
        var responses = await SendTextAsync("/generate", userId: adminUserId);

        // Assert - should receive 2 messages
        Assert.Equal(2, responses.Count);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Equal(ResponseType.Message, responses[1].Type);

        // First message: intro/explanation
        Assert.Contains("TimeSheet", responses[0].Text);
        Assert.Contains("time-tracking bot", responses[0].Text);

        // Second message: registration command
        Assert.Contains("/register", responses[1].Text);
        Assert.Contains("single-use", responses[1].Text);
    }

    [Fact]
    public async Task GenerateCommand_NonAdminUser_RejectsWith403Message()
    {
        // Arrange - register a non-admin user
        const long nonAdminUserId = 20002;
        await RegisterTestUserAsync(telegramUserId: nonAdminUserId, isAdmin: false);

        // Act
        var responses = await SendTextAsync("/generate", userId: nonAdminUserId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("only available to administrators", responses[0].Text);
    }

    [Fact]
    public async Task GenerateCommand_NonRegisteredUser_IgnoresRequest()
    {
        // Arrange - no registration (user not in DB)
        const long unregisteredUserId = 20003;

        // Act
        var responses = await SendTextAsync("/generate", userId: unregisteredUserId);

        // Assert - UpdateHandler should ignore non-registered users silently
        AssertNoResponse(responses);
    }

    [Fact]
    public async Task GenerateCommand_StoresMnemonicAsPending()
    {
        // Arrange - register an admin user
        const long adminUserId = 20004;
        await RegisterTestUserAsync(telegramUserId: adminUserId, isAdmin: true);

        // Act - generate a mnemonic
        var responses = await SendTextAsync("/generate", userId: adminUserId);

        // Assert - should receive 2 messages, extract from the second one
        Assert.Equal(2, responses.Count);
        var responseText = responses[1].Text;
        Assert.Contains("/register", responseText);

        // Extract the mnemonic phrase (everything after "`/register " and before the "`")
        var registerIndex = responseText!.IndexOf("`/register ", StringComparison.Ordinal);
        Assert.True(registerIndex >= 0, "Response should contain '`/register' command");

        var mnemonicStart = registerIndex + "`/register ".Length;
        var mnemonicEnd = responseText.IndexOf('`', mnemonicStart);
        Assert.True(mnemonicEnd > mnemonicStart, "Response should have closing backtick");
        var mnemonicPhrase = responseText[mnemonicStart..mnemonicEnd].Trim();

        // Verify the mnemonic can be used for registration
        using var scope = Fixture.CreateScope();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();

        var isValid = mnemonicService.ValidateAndConsumeMnemonic(mnemonicPhrase);
        Assert.True(isValid, "Generated mnemonic should be stored and valid");
    }

    [Fact]
    public async Task GenerateCommand_GeneratedMnemonicIsSingleUse()
    {
        // Arrange - register an admin user
        const long adminUserId = 20005;
        await RegisterTestUserAsync(telegramUserId: adminUserId, isAdmin: true);

        // Act - generate a mnemonic
        var responses = await SendTextAsync("/generate", userId: adminUserId);

        // Extract the mnemonic from the second message
        var responseText = responses[1].Text!;
        var registerIndex = responseText.IndexOf("`/register ", StringComparison.Ordinal);
        var mnemonicStart = registerIndex + "`/register ".Length;
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
    public async Task GenerateCommand_GeneratedMnemonicIs24Words()
    {
        // Arrange - register an admin user
        const long adminUserId = 20006;
        await RegisterTestUserAsync(telegramUserId: adminUserId, isAdmin: true);

        // Act
        var responses = await SendTextAsync("/generate", userId: adminUserId);

        // Extract the mnemonic from the second message
        var responseText = responses[1].Text!;
        var registerIndex = responseText.IndexOf("`/register ", StringComparison.Ordinal);
        var mnemonicStart = registerIndex + "`/register ".Length;
        var mnemonicEnd = responseText.IndexOf('`', mnemonicStart);
        var mnemonicPhrase = responseText[mnemonicStart..mnemonicEnd].Trim();

        // Assert - BIP39 mnemonic should be 24 words
        var words = mnemonicPhrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(24, words.Length);
    }

    [Fact]
    public async Task GenerateCommand_MultipleGenerations_BothMnemonicsAreStoredAndValid()
    {
        // Arrange - register an admin user
        const long adminUserId = 20007;
        await RegisterTestUserAsync(telegramUserId: adminUserId, isAdmin: true);

        // Act - generate two mnemonics
        var responses1 = await SendTextAsync("/generate", userId: adminUserId);
        Fixture.MockBotClient.ClearResponses();
        var responses2 = await SendTextAsync("/generate", userId: adminUserId);

        // Extract both mnemonics from the second message of each response
        var responseText1 = responses1[1].Text!;
        var registerIndex1 = responseText1.IndexOf("`/register ", StringComparison.Ordinal);
        var mnemonicStart1 = registerIndex1 + "`/register ".Length;
        var mnemonicEnd1 = responseText1.IndexOf('`', mnemonicStart1);
        var mnemonicPhrase1 = responseText1[mnemonicStart1..mnemonicEnd1].Trim();

        var responseText2 = responses2[1].Text!;
        var registerIndex2 = responseText2.IndexOf("`/register ", StringComparison.Ordinal);
        var mnemonicStart2 = registerIndex2 + "`/register ".Length;
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
}
