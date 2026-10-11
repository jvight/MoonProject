using System;
using System.IO;
using UnityEngine;

namespace MoonProject.Core.Save
{
    /// <summary>
    /// One-time carry-over from the game's old name: renaming the product moved <c>persistentDataPath</c> from
    /// ".../MoonProject/Lofi Lunar" to ".../MoonProject/Lunarion", so a player's saves and settings would look lost.
    /// When the new Saves folder holds nothing and the old one does, its files are copied across. The old folder is
    /// never touched, and an existing file is never overwritten.
    /// </summary>
    public static class LegacySaveMigration
    {
        /// <summary>Folder name of the old product name, a sibling of the current persistentDataPath.</summary>
        public const string LegacyProductFolder = "Lofi Lunar";

        /// <summary>Copies the old product's saves into the current one at boot; returns the files copied.</summary>
        public static int Run()
        {
            string current = Application.persistentDataPath.TrimEnd(Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            string parent = Path.GetDirectoryName(current);
            return string.IsNullOrEmpty(parent)
                ? 0
                : Run(Path.Combine(parent, LegacyProductFolder), current);
        }

        /// <summary>Copies <paramref name="legacyRoot"/>/Saves into <paramref name="currentRoot"/>/Saves.</summary>
        public static int Run(string legacyRoot, string currentRoot)
        {
            string from = Path.Combine(legacyRoot, SaveService.FolderName);
            string to = Path.Combine(currentRoot, SaveService.FolderName);
            if (!Directory.Exists(from) || HasFiles(to))
            {
                return 0;
            }

            int copied = 0;
            try
            {
                Directory.CreateDirectory(to);
                foreach (string file in Directory.GetFiles(from))
                {
                    File.Copy(file, Path.Combine(to, Path.GetFileName(file)), false);
                    copied++;
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogError($"{nameof(LegacySaveMigration)}: could not copy {from} to {to} " +
                               $"after {copied} file(s): {exception.Message}");
            }

            if (copied > 0)
            {
                Debug.Log($"{nameof(LegacySaveMigration)}: copied {copied} file(s) from {from} to {to}.");
            }

            return copied;
        }

        private static bool HasFiles(string directory)
        {
            return Directory.Exists(directory) && Directory.GetFiles(directory).Length > 0;
        }
    }
}
