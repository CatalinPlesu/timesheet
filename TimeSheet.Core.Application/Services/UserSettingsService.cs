using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Core.Application.Services;

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
        return await UpdateLunchReminderTimeAsync(telegramUserId, hour, 0, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> UpdateLunchReminderTimeAsync(
        long telegramUserId,
        int? hour,
        int minute,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        user.UpdateLunchReminderTime(hour, minute);
        await unitOfWork.CompleteAsync(cancellationToken);

        return user;
    }

    /// <inheritdoc/>
    public async Task<User?> UpdateTargetWorkHoursAsync(
        long telegramUserId,
        decimal? hours,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        user.UpdateTargetWorkHours(hours);
        await unitOfWork.CompleteAsync(cancellationToken);

        return user;
    }

    /// <inheritdoc/>
    public async Task<User?> UpdateTargetOfficeHoursAsync(
        long telegramUserId,
        decimal? hours,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        user.UpdateTargetOfficeHours(hours);
        await unitOfWork.CompleteAsync(cancellationToken);

        return user;
    }

    /// <inheritdoc/>
    public async Task<User?> UpdateForgotShutdownThresholdAsync(
        long telegramUserId,
        int? thresholdPercent,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        user.UpdateForgotShutdownThreshold(thresholdPercent);
        await unitOfWork.CompleteAsync(cancellationToken);

        return user;
    }
}
