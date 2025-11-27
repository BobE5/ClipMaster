using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace ClipMaster.Services
{
    public class SoundService : IDisposable
    {
        private SoundPlayer? _clipSound;
        private SoundPlayer? _errorSound;
        private bool _isEnabled = true;
        private string? _customClipSoundPath;

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
                // Try to load custom sounds from app directory
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var clipSoundPath = Path.Combine(appDir, "Sounds", "clip.wav");
                var errorSoundPath = Path.Combine(appDir, "Sounds", "error.wav");

                if (File.Exists(clipSoundPath))
                {
                    _clipSound = new SoundPlayer(clipSoundPath);
                    _clipSound.Load();
                    _customClipSoundPath = clipSoundPath;
                }

                if (File.Exists(errorSoundPath))
                {
                    _errorSound = new SoundPlayer(errorSoundPath);
                    _errorSound.Load();
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

                _clipSound?.Dispose();
                _clipSound = new SoundPlayer(filePath);
                _clipSound.Load();
                _customClipSoundPath = filePath;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting clip sound: {ex.Message}");
                return false;
            }
        }

        public void TestClipSound()
        {
            Task.Run(() =>
            {
                try
                {
                    if (_clipSound != null)
                    {
                        _clipSound.Play();
                    }
                    else
                    {
                        SystemSounds.Asterisk.Play();
                    }
                }
                catch
                {
                    // Ignore sound errors
                }
            });
        }

        public void PlayClipSound()
        {
            if (!_isEnabled) return;

            Task.Run(() =>
            {
                try
                {
                    if (_clipSound != null)
                    {
                        _clipSound.Play();
                    }
                    else
                    {
                        // Fallback to system sound
                        SystemSounds.Asterisk.Play();
                    }
                }
                catch
                {
                    // Ignore sound errors
                }
            });
        }

        public void PlayErrorSound()
        {
            if (!_isEnabled) return;

            Task.Run(() =>
            {
                try
                {
                    if (_errorSound != null)
                    {
                        _errorSound.Play();
                    }
                    else
                    {
                        SystemSounds.Exclamation.Play();
                    }
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
            _clipSound?.Dispose();
            _errorSound?.Dispose();
        }
    }
}
