using Plugin.Maui.Audio;

namespace AppScanner.Services;

public class SoundService
{
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _resultPlayer;
    private readonly object _resultLock = new();

    public SoundService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public async Task PlayResultSound(SoundType soundType)
    {
        string fileName = soundType switch
        {
            SoundType.Success => "success.mp3",
            SoundType.Error => "error.mp3",
            SoundType.Navigation => "navigation.mp3",
            _ => "navigation.mp3"
        };

        var stream = await FileSystem.OpenAppPackageFileAsync(fileName);

        lock (_resultLock)
        {
            _resultPlayer?.Stop();
            _resultPlayer?.Dispose();
            _resultPlayer = _audioManager.CreatePlayer(stream);
            _resultPlayer.Volume = 1.0;
            _resultPlayer.Play();
        }
    }
}

public enum SoundType
{
    CameraShot,
    Success,
    Error,
    Navigation
}
