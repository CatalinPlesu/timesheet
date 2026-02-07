namespace TimeSheet.Core.Application.Services;

using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

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
    public async Task<User?> UpdateAutoShutdownLimitAsync(
        long telegramUserId,
        TrackingState state,
        decimal? maxHours,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        switch (state)
        {
            case TrackingState.Working:
                user.UpdateWorkLimit(maxHours);
                break;
            case TrackingState.Commuting:
                user.UpdateCommuteLimit(maxHours);
                break;
            case TrackingState.Lunch:
                user.UpdateLunchLimit(maxHours);
                break;
            case TrackingState.Idle:
                throw new ArgumentException("Cannot set auto-shutdown limit for Idle state.", nameof(state));
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown tracking state.");
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        return user;
    }

    /// <inheritdoc/>
    public async Task<User?> GetUserAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        return await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> UpdateLunchReminderHourAsync(
        long telegramUserId,
        int? hour,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        user.UpdateLunchReminderHour(hour);
        await unitOfWork.CompleteAsync(cancellationToken);

        return user;
    }
}
