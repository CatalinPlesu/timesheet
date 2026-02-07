namespace TimeSheet.Core.Application.Services;

using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Domain.Entities;

/// <summary>
/// Service for managing user settings.
/// </summary>
public sealed class UserSettingsService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IUserSettingsService
{
    /// <inheritdoc/>
    public async Task<User?> UpdateUtcOffsetAsync(
        long telegramUserId,
        int utcOffsetMinutes,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        user.UpdateUtcOffset(utcOffsetMinutes);
        await unitOfWork.CompleteAsync(cancellationToken);

        return user;
    }

    /// <inheritdoc/>
    public async Task<User?> GetUserAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        return await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
    }
}
