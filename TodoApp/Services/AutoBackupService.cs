using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TodoApp.Data;

namespace TodoApp.Services;

/// <summary>
/// Background service that automatically creates a backup on the configured interval.
/// Runs independently of the UI — only fires when AutoBackupEnabled = true.
/// </summary>
public sealed class AutoBackupService : BackgroundService
{
    private readonly IBackupService           _backup;
    private readonly IAppDataStore            _store;
    private readonly ILogger<AutoBackupService> _logger;

    public AutoBackupService(
        IBackupService backup,
        IAppDataStore  store,
        ILogger<AutoBackupService> logger)
    {
        _backup = backup;
        _store  = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait until the app has fully started before first check
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var settings = await _store.GetSettingsAsync();

                if (settings.Backup.AutoBackupEnabled)
                {
                    _logger.LogInformation("Auto-backup: creating backup…");
                    var path = await _backup.CreateBackupAsync();
                    _logger.LogInformation("Auto-backup created: {Path}", path);
                }

                // Re-read settings each cycle so interval changes take effect immediately
                settings = await _store.GetSettingsAsync();
                var hours    = Math.Max(1, settings.Backup.AutoBackupIntervalHours);
                var interval = TimeSpan.FromHours(hours);

                _logger.LogDebug("Auto-backup: next run in {Hours}h.", hours);
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-backup failed.");
                // Back off for 10 minutes on failure
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
