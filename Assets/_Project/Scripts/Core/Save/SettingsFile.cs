using System;
using System.IO;
using UnityEngine;

namespace MoonProject.Core.Save
{
    /// <summary>
    /// <see cref="ISettingsFile"/> as "&lt;slot&gt;.settings.json" (and its ".bak") in the save folder, written with
    /// the same crash-safe swap as the save. Per slot, so a playtest or test slot never touches the player's settings.
    /// </summary>
    public sealed class SettingsFile : ISettingsFile
    {
        public const string Suffix = ".settings";

        public SettingsFile(string directory, string slot)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("The settings file needs the save folder.", nameof(directory));
            }

            if (!SaveService.IsValidSlot(slot))
            {
                throw new ArgumentException($"'{slot}' is not a valid save slot.", nameof(slot));
            }

            FilePath = Path.Combine(directory, slot + Suffix + SaveService.Extension);
            BackupPath = FilePath + SaveService.BackupSuffix;
        }

        public string FilePath { get; }

        public string BackupPath { get; }

        public bool TryRead(out string json)
        {
            return TryReadFile(FilePath, out json) || TryReadFile(BackupPath, out json);
        }

        public bool Write(string json)
        {
            try
            {
                AtomicFile.Write(FilePath, BackupPath, json);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogError($"{nameof(SettingsFile)}: could not write {FilePath}: {exception.Message}");
                return false;
            }
        }

        private static bool TryReadFile(string path, out string json)
        {
            json = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                json = File.ReadAllText(path);
                return !string.IsNullOrWhiteSpace(json);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogError($"{nameof(SettingsFile)}: could not read {path}: {exception.Message}");
                return false;
            }
        }
    }
}
