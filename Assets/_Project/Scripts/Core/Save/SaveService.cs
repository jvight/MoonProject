using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace MoonProject.Core.Save
{
    /// <summary>
    /// File-backed <see cref="ISaveService"/>: one versioned JSON envelope per slot at
    /// <c>&lt;directory&gt;/&lt;slot&gt;.json</c>, holding each registered section's JSON and schema version.
    /// <para><b>Writes</b> are atomic (see <see cref="AtomicFile"/>) and keep the previous file as
    /// <c>&lt;slot&gt;.json.bak</c>. Every section is captured before anything touches the disk, so a failing section
    /// means nothing is written.</para>
    /// <para><b>Recovery, not fallback:</b> if the save file cannot be read it is logged as an error, moved aside to
    /// <c>&lt;slot&gt;.json.corrupt-&lt;timestamp&gt;</c> (never deleted, so it can be inspected) and the backup is
    /// loaded with a warning. If the backup is unreadable too it is moved aside as well and the game starts fresh,
    /// again with an error. A file from a newer build is left untouched and saving is disabled for the session.</para>
    /// <para><b>Never losing data:</b> sections that are saved but not registered this session, sections saved by a
    /// newer schema, and sections whose migration or restore failed are written back verbatim on every save.</para>
    /// <para><b>Content version:</b> every save records the content version of the build that wrote it. A save with
    /// none, or older than <see cref="OldestCompatibleContent"/>, holds progress this content no longer means (an
    /// ability earned before it was gated anew), so it is put away as <c>&lt;slot&gt;.old-&lt;stamp&gt;.json</c>
    /// (never deleted) and the game starts fresh with a warning; <see cref="PutAway"/> does the same for a new game.
    /// </para>
    /// </summary>
    public sealed class SaveService : ISaveService
    {
        /// <summary>Envelope format written by this build.</summary>
        public const int FormatVersion = 1;

        public const string FolderName = "Saves";
        public const string Extension = ".json";
        public const string BackupSuffix = ".bak";
        public const string CorruptMarker = ".corrupt-";
        public const string PutAwayMarker = ".old-";

        /// <summary>
        /// Saves written by content older than this are put away rather than loaded: raise it with a release whose
        /// content breaks older progress.
        /// </summary>
        public const string OldestCompatibleContent = "0.4.1";

        private readonly Dictionary<string, ISaveSection> _sections =
            new Dictionary<string, ISaveSection>(StringComparer.Ordinal);

        private readonly Dictionary<string, SaveEntry> _keptEntries =
            new Dictionary<string, SaveEntry>(StringComparer.Ordinal);

        private readonly HashSet<string> _unappliedKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly string _directory;
        private readonly string _slot;
        private readonly string _contentVersion;
        private bool _readOnly;
        private bool _putAway;

        /// <summary>A save service writing this build's <c>Application.version</c> as the content version.</summary>
        public SaveService(string directory, string slot) : this(directory, slot, Application.version)
        {
        }

        /// <param name="directory">Folder of the save files, e.g. <see cref="DefaultDirectory"/>.</param>
        /// <param name="slot">File name without extension; letters, digits, '-' and '_' only.</param>
        /// <param name="contentVersion">
        /// The content version saves are written with (dotted numbers); never older than
        /// <see cref="OldestCompatibleContent"/>, or the next launch would put away everything this one saves.
        /// </param>
        public SaveService(string directory, string slot, string contentVersion)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Save directory is empty.", nameof(directory));
            }

            if (!IsValidSlot(slot))
            {
                throw new ArgumentException($"Invalid save slot '{slot}': use letters, digits, '-' and '_'.",
                    nameof(slot));
            }

            if (!ContentVersion.IsValid(contentVersion))
            {
                throw new ArgumentException($"Content version '{contentVersion}' is not dotted numbers.",
                    nameof(contentVersion));
            }

            if (!ContentVersion.IsAtLeast(contentVersion, OldestCompatibleContent))
            {
                throw new ArgumentException($"Content version {contentVersion} is older than " +
                                            $"{nameof(OldestCompatibleContent)} {OldestCompatibleContent}: every " +
                                            "save it wrote would be put away on the next launch.",
                    nameof(contentVersion));
            }

            FilePath = Path.GetFullPath(Path.Combine(directory, slot + Extension));
            BackupPath = FilePath + BackupSuffix;
            _directory = Path.GetDirectoryName(FilePath);
            _slot = slot;
            _contentVersion = contentVersion;
        }

        /// <summary>&lt;persistentDataPath&gt;/Saves.</summary>
        public static string DefaultDirectory => Path.Combine(Application.persistentDataPath, FolderName);

        public string FilePath { get; }

        public string BackupPath { get; }

        public bool IsLoaded { get; private set; }

        public SaveLoadResult LoadResult { get; private set; }

        public static bool IsValidSlot(string slot)
        {
            if (string.IsNullOrEmpty(slot))
            {
                return false;
            }

            foreach (char c in slot)
            {
                if (!(char.IsLetterOrDigit(c) && c < 128) && c != '-' && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        public IDisposable Register(ISaveSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            if (string.IsNullOrWhiteSpace(section.Key))
            {
                throw new ArgumentException($"{section.GetType().Name} has an empty save key.", nameof(section));
            }

            if (section.Version < 1)
            {
                throw new ArgumentException($"Save section '{section.Key}' has version {section.Version}; " +
                                            "versions start at 1.", nameof(section));
            }

            if (_sections.ContainsKey(section.Key))
            {
                throw new InvalidOperationException($"Save section '{section.Key}' is already registered.");
            }

            _sections.Add(section.Key, section);
            if (IsLoaded && _keptEntries.TryGetValue(section.Key, out SaveEntry entry) &&
                !_unappliedKeys.Contains(section.Key))
            {
                Apply(section, entry);
            }

            return new Registration(this, section);
        }

        /// <summary>
        /// Reads the save file once and restores every registered section (later registrations are restored when
        /// they register). Call after all systems are initialised.
        /// </summary>
        public SaveLoadResult Load()
        {
            if (IsLoaded)
            {
                throw new InvalidOperationException("The save file was already loaded.");
            }

            IsLoaded = true;
            LoadResult = ReadEnvelope(out SaveEnvelope envelope);
            if (envelope != null && !ContentVersion.IsAtLeast(envelope.contentVersion, OldestCompatibleContent))
            {
                string archived = Archive();
                Debug.LogWarning($"Save file {FilePath} holds content version " +
                                 $"'{envelope.contentVersion ?? "none"}', older than {OldestCompatibleContent}: it " +
                                 $"was put away to {archived} and a new game starts.");
                envelope = null;
                LoadResult = SaveLoadResult.PutAwayOlder;
            }

            if (envelope == null)
            {
                return LoadResult;
            }

            foreach (SaveEntry entry in envelope.sections)
            {
                if (_keptEntries.ContainsKey(entry.key))
                {
                    Debug.LogError($"Save file {FilePath}: section '{entry.key}' appears twice; the first is used.");
                    continue;
                }

                _keptEntries.Add(entry.key, entry);
            }

            var registered = new List<ISaveSection>(_sections.Values);
            registered.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            foreach (ISaveSection section in registered)
            {
                if (_keptEntries.TryGetValue(section.Key, out SaveEntry entry))
                {
                    Apply(section, entry);
                }
            }

            return LoadResult;
        }

        public bool SaveNow()
        {
            if (!IsLoaded)
            {
                Debug.LogError($"SaveNow before Load would overwrite {FilePath} with unread progress; nothing saved.");
                return false;
            }

            if (_readOnly)
            {
                Debug.LogError($"{FilePath} was written by a newer build; saving is disabled to protect it.");
                return false;
            }

            if (_putAway)
            {
                Debug.LogWarning($"{FilePath} was put away for a new game; this session saves nothing more.");
                return false;
            }

            var entries = new SortedDictionary<string, SaveEntry>(_keptEntries, StringComparer.Ordinal);
            foreach (ISaveSection section in _sections.Values)
            {
                if (_unappliedKeys.Contains(section.Key))
                {
                    continue;
                }

                try
                {
                    entries[section.Key] = new SaveEntry
                    {
                        key = section.Key, version = section.Version, json = section.Capture(),
                    };
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    Debug.LogError($"Save section '{section.Key}' failed to capture; nothing was saved.");
                    return false;
                }
            }

            var envelope = new SaveEnvelope
            {
                formatVersion = FormatVersion,
                contentVersion = _contentVersion,
                savedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                sections = new SaveEntry[entries.Count],
            };
            entries.Values.CopyTo(envelope.sections, 0);
            try
            {
                AtomicFile.Write(FilePath, BackupPath, JsonUtility.ToJson(envelope, true));
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogError($"Writing {FilePath} failed: {exception.Message}. The previous save is unchanged.");
                return false;
            }
        }

        public string PutAway()
        {
            string archived = Archive();
            _keptEntries.Clear();
            _unappliedKeys.Clear();
            _putAway = true;
            Debug.Log(archived != null
                ? $"New game: {FilePath} was put away to {archived}."
                : $"New game: {FilePath} did not exist yet; nothing to put away.");
            return archived;
        }

        /// <summary>
        /// Moves the save and its backup to &lt;slot&gt;.old-&lt;utc stamp&gt;.json(.bak), under a stamp neither is
        /// taken under. Returns where the save (else the backup) went, or null when neither exists.
        /// </summary>
        private string Archive()
        {
            bool main = File.Exists(FilePath);
            bool backup = File.Exists(BackupPath);
            if (!main && !backup)
            {
                return null;
            }

            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string target = Path.Combine(_directory, _slot + PutAwayMarker + stamp + Extension);
            for (int i = 1; File.Exists(target) || File.Exists(target + BackupSuffix); i++)
            {
                target = Path.Combine(_directory, $"{_slot}{PutAwayMarker}{stamp}-{i}{Extension}");
            }

            if (backup)
            {
                File.Move(BackupPath, target + BackupSuffix);
            }

            if (!main)
            {
                return target + BackupSuffix;
            }

            File.Move(FilePath, target);
            return target;
        }

        private SaveLoadResult ReadEnvelope(out SaveEnvelope envelope)
        {
            bool mainExists = File.Exists(FilePath);
            bool backupExists = File.Exists(BackupPath);
            if (!mainExists && !backupExists)
            {
                envelope = null;
                return SaveLoadResult.NoSave;
            }

            if (mainExists)
            {
                string problem = TryParse(FilePath, out envelope, out bool newer);
                if (problem == null)
                {
                    return SaveLoadResult.Loaded;
                }

                if (newer)
                {
                    _readOnly = true;
                    envelope = null;
                    Debug.LogError($"{FilePath}: {problem}. It is left untouched and this session will not save.");
                    return SaveLoadResult.NewerFormat;
                }

                string aside = MoveAside(FilePath);
                Debug.LogError($"Save file {FilePath} is unreadable ({problem}). It was moved to {aside}; " +
                               (backupExists ? "loading the backup." : "there is no backup, starting fresh."));
            }

            if (!backupExists)
            {
                envelope = null;
                return SaveLoadResult.Unreadable;
            }

            string backupProblem = TryParse(BackupPath, out envelope, out bool newerBackup);
            if (backupProblem == null)
            {
                Debug.LogWarning($"Recovered progress from the backup {BackupPath}.");
                return SaveLoadResult.RecoveredFromBackup;
            }

            envelope = null;
            if (newerBackup)
            {
                _readOnly = true;
                Debug.LogError($"{BackupPath}: {backupProblem}. It is left untouched and this session will not save.");
                return SaveLoadResult.NewerFormat;
            }

            string backupAside = MoveAside(BackupPath);
            Debug.LogError($"Save backup {BackupPath} is unreadable too ({backupProblem}). It was moved to " +
                           $"{backupAside}; starting with fresh progress.");
            return SaveLoadResult.Unreadable;
        }

        /// <summary>Returns null when <paramref name="path"/> holds a valid envelope, else the reason it does not.</summary>
        private static string TryParse(string path, out SaveEnvelope envelope, out bool newerFormat)
        {
            envelope = null;
            newerFormat = false;
            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return "cannot be read: " + exception.Message;
            }

            SaveEnvelope parsed;
            try
            {
                parsed = JsonUtility.FromJson<SaveEnvelope>(text);
            }
            catch (ArgumentException exception)
            {
                return "invalid JSON: " + exception.Message;
            }

            if (parsed == null || parsed.formatVersion < 1)
            {
                return "not a save file (no formatVersion)";
            }

            if (parsed.formatVersion > FormatVersion)
            {
                newerFormat = true;
                return $"format version {parsed.formatVersion} is newer than this build's {FormatVersion}";
            }

            if (parsed.sections == null)
            {
                return "no sections";
            }

            foreach (SaveEntry entry in parsed.sections)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key) || entry.json == null)
                {
                    return "a section has no key or data";
                }
            }

            envelope = parsed;
            return null;
        }

        private static string MoveAside(string path)
        {
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string target = path + CorruptMarker + stamp;
            for (int i = 1; File.Exists(target); i++)
            {
                target = $"{path}{CorruptMarker}{stamp}-{i}";
            }

            File.Move(path, target);
            return target;
        }

        private void Apply(ISaveSection section, SaveEntry entry)
        {
            if (entry.version > section.Version)
            {
                Unapplied(section, $"it was saved by a newer build (version {entry.version} > {section.Version})");
                return;
            }

            if (entry.version < 1)
            {
                Unapplied(section, $"it has an invalid version {entry.version}");
                return;
            }

            string json = entry.json;
            try
            {
                for (int version = entry.version; version < section.Version; version++)
                {
                    json = section.Migrate(json, version);
                }

                section.Restore(json);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Unapplied(section, $"migrating or restoring version {entry.version} failed");
                return;
            }

            _keptEntries.Remove(section.Key);
        }

        private void Unapplied(ISaveSection section, string reason)
        {
            _unappliedKeys.Add(section.Key);
            Debug.LogError($"Save section '{section.Key}' was not restored: {reason}. It keeps its initial state " +
                           "and its saved data is written back unchanged.");
        }

        private void Unregister(ISaveSection section)
        {
            if (!_sections.TryGetValue(section.Key, out ISaveSection current) || current != section)
            {
                return;
            }

            _sections.Remove(section.Key);
            if (!IsLoaded || _unappliedKeys.Contains(section.Key))
            {
                return;
            }

            try
            {
                _keptEntries[section.Key] = new SaveEntry
                {
                    key = section.Key, version = section.Version, json = section.Capture(),
                };
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError($"Save section '{section.Key}' failed to capture when unregistering; its last " +
                               "saved data is kept instead.");
            }
        }

        private sealed class Registration : IDisposable
        {
            private SaveService _service;
            private ISaveSection _section;

            public Registration(SaveService service, ISaveSection section)
            {
                _service = service;
                _section = section;
            }

            public void Dispose()
            {
                if (_service == null)
                {
                    return;
                }

                _service.Unregister(_section);
                _service = null;
                _section = null;
            }
        }
    }
}
