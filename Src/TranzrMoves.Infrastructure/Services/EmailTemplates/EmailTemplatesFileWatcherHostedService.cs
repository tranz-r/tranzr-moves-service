using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services.EmailTemplates;

/// <summary>
/// In Development, watches <c>Services/EmailTemplates/Templates/**/*.hbs</c> next to the app and calls
/// <see cref="ITemplateService.ReloadTemplates"/> so edits are picked up without restarting the API.
/// </summary>
public sealed class EmailTemplatesFileWatcherHostedService : IHostedService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ITemplateService _templateService;
    private readonly ILogger<EmailTemplatesFileWatcherHostedService> _logger;
    private readonly string _templatesPath = Path.Combine(AppContext.BaseDirectory, "Services", "EmailTemplates", "Templates");
    private FileSystemWatcher? _watcher;
    private readonly object _debounceLock = new();
    private CancellationTokenSource? _debounceCts;

    public EmailTemplatesFileWatcherHostedService(
        IHostEnvironment hostEnvironment,
        ITemplateService templateService,
        ILogger<EmailTemplatesFileWatcherHostedService> logger)
    {
        _hostEnvironment = hostEnvironment;
        _templateService = templateService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            _logger.LogDebug("Email template file watcher is disabled outside the Development environment.");
            return Task.CompletedTask;
        }

        if (!Directory.Exists(_templatesPath))
        {
            _logger.LogWarning("Email templates directory not found for file watcher: {Path}", _templatesPath);
            return Task.CompletedTask;
        }

        _watcher = new FileSystemWatcher(_templatesPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            Filter = "*.hbs",
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnTemplateFileChanged;
        _watcher.Created += OnTemplateFileChanged;
        _watcher.Renamed += OnTemplateFileRenamed;
        _logger.LogInformation("Watching {Path} for .hbs changes to hot-reload email templates.", _templatesPath);
        return Task.CompletedTask;
    }

    private void OnTemplateFileRenamed(object sender, RenamedEventArgs e) => ScheduleReload();

    private void OnTemplateFileChanged(object sender, FileSystemEventArgs e) => ScheduleReload();

    private void ScheduleReload()
    {
        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            _ = DebouncedReloadAsync(token);
        }
    }

    private async Task DebouncedReloadAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            _templateService.ReloadTemplates();
            _logger.LogInformation("Email templates reloaded after disk change.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload email templates after disk change.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnTemplateFileChanged;
            _watcher.Created -= OnTemplateFileChanged;
            _watcher.Renamed -= OnTemplateFileRenamed;
            _watcher.Dispose();
            _watcher = null;
        }

        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }

        return Task.CompletedTask;
    }
}
