using System;
using UnityEngine;
using MoonProject.Core.Save;

namespace MoonProject.UI
{
    /// <summary>
    /// Keeps <see cref="PlayerSettings"/> in the settings file beside the save, so New Game never resets them. Saves
    /// written before the file existed carried the settings in their "ui.settings" section: those are read once, when
    /// the file has nothing yet, and written to the file.
    /// </summary>
    internal sealed class SettingsStore
    {
        private readonly PlayerSettings _player;
        private readonly ISettingsFile _file;

        public SettingsStore(PlayerSettings player, ISettingsFile file)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        /// <summary>True once the file has given (or been given) the settings.</summary>
        public bool IsStored { get; private set; }

        /// <summary>Applies the file's settings, if it has any. Call before the save loads.</summary>
        public void Load()
        {
            if (!_file.TryRead(out string json))
            {
                return;
            }

            SettingsSaveData data = JsonUtility.FromJson<SettingsSaveData>(json);
            if (data == null)
            {
                Debug.LogError($"{nameof(SettingsStore)}: the settings file is not settings JSON; defaults are kept.");
                return;
            }

            _player.Restore(data);
            IsStored = true;
        }

        /// <summary>A save's old "ui.settings" section: used only while the file has nothing, then written to it.</summary>
        public void Migrate(SettingsSaveData fromSave)
        {
            if (IsStored)
            {
                return;
            }

            _player.Restore(fromSave);
            Store();
        }

        /// <summary>Writes the settings when the player changed any.</summary>
        public void SaveIfChanged()
        {
            if (_player.IsDirty)
            {
                Store();
            }
        }

        private void Store()
        {
            if (_file.Write(JsonUtility.ToJson(_player.Capture(), true)))
            {
                _player.MarkSaved();
                IsStored = true;
            }
        }
    }
}
