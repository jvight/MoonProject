using System;
using System.IO;

namespace MoonProject.App
{
    /// <summary>
    /// A one-shot note beside the save ("&lt;slot&gt;.fresh") that the next boot should skip the title and start a fresh
    /// game: New Game puts the save away and reloads the scene, and the reloaded bootstrap reads and clears it
    /// (docs/features/M3-09, amendment 3).
    /// </summary>
    public sealed class BootIntent
    {
        private const string Extension = ".fresh";
        private readonly string _path;

        public BootIntent(string directory, string slot)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("A boot intent needs the save directory.", nameof(directory));
            }

            if (string.IsNullOrEmpty(slot))
            {
                throw new ArgumentException("A boot intent needs the save slot.", nameof(slot));
            }

            _path = Path.Combine(directory, slot + Extension);
        }

        public string FilePath => _path;

        /// <summary>The next boot starts fresh.</summary>
        public void Arm()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            File.WriteAllText(_path, string.Empty);
        }

        /// <summary>True (and cleared) when the previous session armed a fresh start.</summary>
        public bool TryConsume()
        {
            if (!File.Exists(_path))
            {
                return false;
            }

            File.Delete(_path);
            return true;
        }
    }
}
