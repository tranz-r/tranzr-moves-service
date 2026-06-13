namespace TranzrMoves.Infrastructure.Services;

public sealed class QuoteRemindersOptions
{
    public const string SectionName = "QuoteReminders";

    public bool Enabled { get; set; } = true;

    public int ScanIntervalMinutes { get; set; } = 60;

    public int IdleHoursBeforeReminder { get; set; } = 24;

    public int ReminderCooldownDays { get; set; } = 7;

    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";

    public int ResumeTokenLifetimeDays { get; set; } = 30;
}
