using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using NAudio.Wave;

namespace ClipMaster.Services
{
    public class SoundService : IDisposable
    {
        private SoundPlayer? _wavPlayer;
        private string? _customClipSoundPath;
        private bool _isEnabled = true;
        private bool _isMp3;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public string? CurrentSoundPath => _customClipSoundPath;

        public SoundService()
        {
            InitializeSounds();
        }

        private void InitializeSounds()
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;

                // Try MP3 first, then WAV
                var clipMp3Path = Path.Combine(appDir, "Sounds", "clip.mp3");
                var clipWavPath = Path.Combine(appDir, "Sounds", "clip.wav");

                if (File.Exists(clipMp3Path))
                {
                    _customClipSoundPath = clipMp3Path;
                    _isMp3 = true;
                }
                else if (File.Exists(clipWavPath))
                {
                    _wavPlayer = new SoundPlayer(clipWavPath);
                    _wavPlayer.Load();
                    _customClipSoundPath = clipWavPath;
                    _isMp3 = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading sounds: {ex.Message}");
            }
        }

        public bool SetClipSound(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var ext = Path.GetExtension(filePath).ToLowerInvariant();

                if (ext == ".mp3")
                {
                    // Test that we can read it
                    using (var reader = new Mp3FileReader(filePath))
                    {
                        // Just verify it opens
                    }
                    _wavPlayer?.Dispose();
                    _wavPlayer = null;
                    _customClipSoundPath = filePath;
                    _isMp3 = true;
                    return true;
                }
                else if (ext == ".wav")
                {
                    _wavPlayer?.Dispose();
                    _wavPlayer = new SoundPlayer(filePath);
                    _wavPlayer.Load();
                    _customClipSoundPath = filePath;
                    _isMp3 = false;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting clip sound: {ex.Message}");
                return false;
            }
        }

        private void PlaySound()
        {
            try
            {
                if (_customClipSoundPath == null)
                {
                    SystemSounds.Asterisk.Play();
                    return;
                }

                if (_isMp3)
                {
                    using var reader = new Mp3FileReader(_customClipSoundPath);
                    using var waveOut = new WaveOutEvent();
                    waveOut.Init(reader);
                    waveOut.Play();
                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(50);
                    }
                }
                else if (_wavPlayer != null)
                {
                    _wavPlayer.Play();
                }
                else
                {
                    SystemSounds.Asterisk.Play();
                }
            }
            catch
            {
                SystemSounds.Asterisk.Play();
            }
        }

        public void TestClipSound()
        {
            Task.Run(() => PlaySound());
        }

        public void PlayClipSound()
        {
            if (!_isEnabled) return;
            Task.Run(() => PlaySound());
        }

        public void PlayErrorSound()
        {
            if (!_isEnabled) return;
            Task.Run(() =>
            {
                try
                {
                    SystemSounds.Exclamation.Play();
                }
                catch
                {
                    // Ignore sound errors
                }
            });
        }

        public void PlaySuccessSound()
        {
            if (!_isEnabled) return;
            Task.Run(() =>
            {
                try
                {
                    SystemSounds.Asterisk.Play();
                }
                catch
                {
                    // Ignore sound errors
                }
            });
        }

        public void Dispose()
        {
            _wavPlayer?.Dispose();
        }
    }
}
