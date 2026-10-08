using System;
using UnityEngine;

namespace MoonProject.Core.Save
{
    /// <summary>
    /// <see cref="ISaveSection"/> over a plain <c>[Serializable]</c> data class serialised with JsonUtility:
    /// <code>
    /// _saveToken = context.Get&lt;ISaveService&gt;().Register(new SaveSection&lt;MaterialsSaveData&gt;(
    ///     "gameplay.wallet", 2, () =&gt; new MaterialsSaveData { metal = _metal }, data =&gt; _metal = data.metal,
    ///     MaterialsSaveMigrations.Migrate));
    /// </code>
    /// <paramref name="migrate"/> receives JSON at version N and returns JSON at N + 1 (keep old DTO classes around
    /// to read old shapes). Without it, loading an older version fails loudly for this section only.
    /// </summary>
    public sealed class SaveSection<T> : ISaveSection where T : class, new()
    {
        private readonly Func<T> _capture;
        private readonly Action<T> _restore;
        private readonly Func<string, int, string> _migrate;

        public SaveSection(string key, int version, Func<T> capture, Action<T> restore,
            Func<string, int, string> migrate = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Save section key is empty.", nameof(key));
            }

            if (version < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(version), version, "Save section versions start at 1.");
            }

            Key = key;
            Version = version;
            _capture = capture ?? throw new ArgumentNullException(nameof(capture));
            _restore = restore ?? throw new ArgumentNullException(nameof(restore));
            _migrate = migrate;
        }

        public string Key { get; }

        public int Version { get; }

        public string Capture()
        {
            T data = _capture();
            if (data == null)
            {
                throw new InvalidOperationException($"Save section '{Key}' captured null.");
            }

            return JsonUtility.ToJson(data);
        }

        public void Restore(string json)
        {
            T data = JsonUtility.FromJson<T>(json);
            if (data == null)
            {
                throw new FormatException($"Save section '{Key}' holds no data.");
            }

            _restore(data);
        }

        public string Migrate(string json, int fromVersion)
        {
            if (_migrate == null)
            {
                throw new InvalidOperationException(
                    $"Save section '{Key}' has no migration from version {fromVersion} to {fromVersion + 1}.");
            }

            return _migrate(json, fromVersion);
        }
    }
}
