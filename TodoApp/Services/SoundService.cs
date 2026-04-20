using System.IO;
using System.Media;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using TodoApp.Core.Enums;
using TodoApp.Data;

namespace TodoApp.Services;

/// <summary>
/// Plays notification and completion sounds via NAudio.
/// Falls back to Windows SystemSounds when a custom file is missing.
/// Always safe to call — exceptions are logged and swallowed.
/// </summary>
public sealed class SoundService : ISoundService, IDisposable
{
    private readonly IAppDataStore       _store;
    private readonly ILogger<SoundService> _logger;

    private IWavePlayer?    _player;
    private AudioFileReader? _reader;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsEnabled { get; private set; } = true;

    public SoundService(IAppDataStore store, ILogger<SoundService> logger)
    {
        _store  = store;
        _logger = logger;
    }

    public async Task PlayFileAsync(string filePath, float volume = 1f)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(filePath)) return;
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("Sound file not found: {Path}", filePath);
            return;
        }

        await _lock.WaitAsync();
        try
        {
            StopCurrentNoLock();

            _reader = new AudioFileReader(filePath) { Volume = Math.Clamp(volume, 0f, 1f) };
            _player = new WasapiOut(AudioClientShareMode.Shared, 80);
            _player.Init(_reader);
            _player.Play();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to play sound file: {Path}", filePath);
        }
        finally { _lock.Release(); }
    }

    public async Task PlayPriorityAsync(Priority priority)
    {
        var settings = await _store.GetSettingsAsync();
        var sound    = settings.Sound;

        if (!sound.Enabled) return;

        if (sound.PrioritySounds.TryGetValue(priority, out var path) &&
            !string.IsNullOrWhiteSpace(path))
        {
            await PlayFileAsync(path, sound.Volume);
            return;
        }

        // Fallback: Windows system sound based on priority
        await Task.Run(() =>
        {
            try
            {
                switch (priority)
                {
                    case Priority.Critical: SystemSounds.Hand.Play();      break;
                    case Priority.High:     SystemSounds.Exclamation.Play(); break;
                    default:                SystemSounds.Asterisk.Play();   break;
                }
            }
            catch { /* ignore on headless systems */ }
        });
    }

    public async Task PlayCompletionAsync()
    {
        var settings = await _store.GetSettingsAsync();
        var sound    = settings.Sound;

        if (!sound.Enabled) return;

        if (!string.IsNullOrWhiteSpace(sound.CompletionSoundPath) &&
            File.Exists(sound.CompletionSoundPath))
        {
            await PlayFileAsync(sound.CompletionSoundPath, sound.Volume);
            return;
        }

        await Task.Run(() =>
        {
            try { SystemSounds.Asterisk.Play(); }
            catch { }
        });
    }

    public void StopAll()
    {
        _lock.Wait();
        try { StopCurrentNoLock(); }
        finally { _lock.Release(); }
    }

    private void StopCurrentNoLock()
    {
        try { _player?.Stop(); } catch { }
        _player?.Dispose();
        _reader?.Dispose();
        _player = null;
        _reader = null;
    }

    public void Dispose()
    {
        StopAll();
        _lock.Dispose();
    }
}
