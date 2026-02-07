namespace TimeSheet.Core.Domain.Entities;

using TimeSheet.Core.Domain.SharedKernel;

/// <summary>
/// Represents a registered user of the TimeSheet bot.
/// </summary>
public sealed class User : CreatedEntity
{
    /// <summary>
    /// Gets the Telegram user ID.
    /// </summary>
    public long TelegramUserId { get; init; }

    /// <summary>
    /// Gets the Telegram username (optional).
    /// </summary>
    public string? TelegramUsername { get; init; }

    /// <summary>
    /// Gets a value indicating whether this user is an administrator.
    /// The first user to register becomes the admin.
    /// </summary>
    public bool IsAdmin { get; init; }

    /// <summary>
    /// Gets the user's UTC offset in minutes.
    /// Used for displaying times in the user's local timezone.
    /// Example: +60 for UTC+1, -300 for UTC-5.
    /// </summary>
    public int UtcOffsetMinutes { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this user registered.
    /// </summary>
    public DateTimeOffset RegisteredAt { get; init; }

    /// <summary>
    /// Gets the maximum allowed hours for a work session before auto-shutdown.
    /// Null means no limit.
    /// </summary>
    public decimal? MaxWorkHours { get; private set; }

    /// <summary>
    /// Gets the maximum allowed hours for a commute session before auto-shutdown.
    /// Null means no limit.
    /// </summary>
    public decimal? MaxCommuteHours { get; private set; }

    /// <summary>
    /// Gets the maximum allowed hours for a lunch session before auto-shutdown.
    /// Null means no limit.
    /// </summary>
    public decimal? MaxLunchHours { get; private set; }

    /// <summary>
    /// Gets the hour (0-23) at which to send a lunch reminder.
    /// Null means no lunch reminder is configured.
    /// </summary>
    public int? LunchReminderHour { get; private set; }

    /// <summary>
    /// Gets the target work hours per day.
    /// When reached, the user will be notified once.
    /// Null means no target is configured.
    /// </summary>
    public decimal? TargetWorkHours { get; private set; }

    /// <summary>
    /// Gets the threshold percentage for forgot-to-shutdown detection.
    /// When a session exceeds average duration by this percentage, a reminder is sent.
    /// Null means no forgot-shutdown detection is configured.
    /// Default is 150 (150% of average).
    /// </summary>
    public int? ForgotShutdownThresholdPercent { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// Used when creating a new user during registration.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="telegramUsername">The Telegram username (optional).</param>
    /// <param name="isAdmin">Whether this user is an administrator.</param>
    /// <param name="utcOffsetMinutes">The user's UTC offset in minutes.</param>
    public User(long telegramUserId, string? telegramUsername, bool isAdmin, int utcOffsetMinutes)
        : base()
    {
        TelegramUserId = telegramUserId;
        TelegramUsername = telegramUsername;
        IsAdmin = isAdmin;
        UtcOffsetMinutes = utcOffsetMinutes;
        RegisteredAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// Used for entity rehydration from persistence.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="createdAt">The UTC timestamp when this entity was created.</param>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="telegramUsername">The Telegram username (optional).</param>
    /// <param name="isAdmin">Whether this user is an administrator.</param>
    /// <param name="utcOffsetMinutes">The user's UTC offset in minutes.</param>
    /// <param name="registeredAt">The UTC timestamp when this user registered.</param>
    /// <param name="maxWorkHours">The maximum allowed hours for a work session (null = no limit).</param>
    /// <param name="maxCommuteHours">The maximum allowed hours for a commute session (null = no limit).</param>
    /// <param name="maxLunchHours">The maximum allowed hours for a lunch session (null = no limit).</param>
    /// <param name="lunchReminderHour">The hour (0-23) at which to send a lunch reminder (null = no reminder).</param>
    /// <param name="targetWorkHours">The target work hours per day (null = no target).</param>
    /// <param name="forgotShutdownThresholdPercent">The threshold percentage for forgot-shutdown detection (null = no detection).</param>
    public User(
        Guid id,
        DateTimeOffset createdAt,
        long telegramUserId,
        string? telegramUsername,
        bool isAdmin,
        int utcOffsetMinutes,
        DateTimeOffset registeredAt,
        decimal? maxWorkHours = null,
        decimal? maxCommuteHours = null,
        decimal? maxLunchHours = null,
        int? lunchReminderHour = null,
        decimal? targetWorkHours = null,
        int? forgotShutdownThresholdPercent = null)
        : base(id, createdAt)
    {
        TelegramUserId = telegramUserId;
        TelegramUsername = telegramUsername;
        IsAdmin = isAdmin;
        UtcOffsetMinutes = utcOffsetMinutes;
        RegisteredAt = registeredAt;
        MaxWorkHours = maxWorkHours;
        MaxCommuteHours = maxCommuteHours;
        MaxLunchHours = maxLunchHours;
        LunchReminderHour = lunchReminderHour;
        TargetWorkHours = targetWorkHours;
        ForgotShutdownThresholdPercent = forgotShutdownThresholdPercent;
    }

    /// <summary>
    /// Updates the user's UTC offset.
    /// </summary>
    /// <param name="utcOffsetMinutes">The new UTC offset in minutes.</param>
    public void UpdateUtcOffset(int utcOffsetMinutes)
    {
        UtcOffsetMinutes = utcOffsetMinutes;
    }

    /// <summary>
    /// Updates the auto-shutdown limit for work sessions.
    /// </summary>
    /// <param name="maxHours">The maximum allowed hours (null = no limit).</param>
    /// <exception cref="ArgumentException">Thrown when maxHours is negative or zero.</exception>
    public void UpdateWorkLimit(decimal? maxHours)
    {
        if (maxHours.HasValue && maxHours.Value <= 0)
            throw new ArgumentException("Maximum hours must be positive.", nameof(maxHours));

        MaxWorkHours = maxHours;
    }

    /// <summary>
    /// Updates the auto-shutdown limit for commute sessions.
    /// </summary>
    /// <param name="maxHours">The maximum allowed hours (null = no limit).</param>
    /// <exception cref="ArgumentException">Thrown when maxHours is negative or zero.</exception>
    public void UpdateCommuteLimit(decimal? maxHours)
    {
        if (maxHours.HasValue && maxHours.Value <= 0)
            throw new ArgumentException("Maximum hours must be positive.", nameof(maxHours));

        MaxCommuteHours = maxHours;
    }

    /// <summary>
    /// Updates the auto-shutdown limit for lunch sessions.
    /// </summary>
    /// <param name="maxHours">The maximum allowed hours (null = no limit).</param>
    /// <exception cref="ArgumentException">Thrown when maxHours is negative or zero.</exception>
    public void UpdateLunchLimit(decimal? maxHours)
    {
        if (maxHours.HasValue && maxHours.Value <= 0)
            throw new ArgumentException("Maximum hours must be positive.", nameof(maxHours));

        MaxLunchHours = maxHours;
    }

    /// <summary>
    /// Updates the lunch reminder hour setting.
    /// </summary>
    /// <param name="hour">The hour (0-23) at which to send a lunch reminder (null = disable reminder).</param>
    /// <exception cref="ArgumentException">Thrown when hour is outside the valid range (0-23).</exception>
    public void UpdateLunchReminderHour(int? hour)
    {
        if (hour.HasValue && (hour.Value < 0 || hour.Value > 23))
            throw new ArgumentException("Hour must be between 0 and 23.", nameof(hour));

        LunchReminderHour = hour;
    }

    /// <summary>
    /// Updates the target work hours per day setting.
    /// </summary>
    /// <param name="hours">The target work hours per day (null = disable target).</param>
    /// <exception cref="ArgumentException">Thrown when hours is negative or zero.</exception>
    public void UpdateTargetWorkHours(decimal? hours)
    {
        if (hours.HasValue && hours.Value <= 0)
            throw new ArgumentException("Target work hours must be positive.", nameof(hours));

        TargetWorkHours = hours;
    }

    /// <summary>
    /// Updates the forgot-shutdown threshold percentage setting.
    /// </summary>
    /// <param name="thresholdPercent">The threshold percentage (e.g., 150 for 150% of average). Null = disable detection.</param>
    /// <exception cref="ArgumentException">Thrown when thresholdPercent is less than or equal to 100.</exception>
    public void UpdateForgotShutdownThreshold(int? thresholdPercent)
    {
        if (thresholdPercent.HasValue && thresholdPercent.Value <= 100)
            throw new ArgumentException("Threshold percentage must be greater than 100.", nameof(thresholdPercent));

        ForgotShutdownThresholdPercent = thresholdPercent;
    }
}
