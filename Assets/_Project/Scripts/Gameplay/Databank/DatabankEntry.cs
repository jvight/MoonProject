using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One databank entry (docs/design/databank.md §2): what 07's page shows and how the entry unlocks. Text fields are
    /// localization keys.
    /// </summary>
    [Serializable]
    public sealed class DatabankEntry
    {
        public const string IdPrefix = "db.";

        [Tooltip("db.<name>: saved with the databank, never renamed.")]
        [SerializeField] private string _id;

        [SerializeField] private DatabankCategory _category;

        [Tooltip("Localization key of the entry's name (at most 24 characters in both languages).")]
        [SerializeField] private string _nameKey;

        [Tooltip("Who made the subject (tech-eras §6); None for places and phenomena.")]
        [SerializeField] private DatabankMaker _maker;

        [Tooltip("The year it was made or placed: 1967, or '1971 · 1984' for two eras.")]
        [SerializeField] private string _year;

        [Tooltip("The era of the subject (tech-eras §1).")]
        [SerializeField] private DatabankEra _era;

        [Tooltip("Localization key of 07's measured line: uppercase, middle-dot separated, two lines of 48 at most.")]
        [SerializeField] private string _readoutKey;

        [Tooltip("Localization key of the crew's catalogue card (1 to 3 sentences, signed); empty means NO LUMEN RECORD.")]
        [SerializeField] private string _historyKey;

        [Tooltip("Model: a prefab turned on the page. Print: a scanned sheet.")]
        [SerializeField] private DatabankViewKind _view;

        [Tooltip("The prefab a Model entry turns on the page.")]
        [SerializeField] private GameObject _model;

        [Tooltip("Node names hidden on the page so the model matches the world (the Scout's removed camera and scoop).")]
        [SerializeField] private string[] _hiddenNodes = Array.Empty<string>();

        [Tooltip("Degrees the model is turned to when the page first frames it.")]
        [Range(-180f, 180f)] [SerializeField] private float _framingYaw;

        [Tooltip("The scanned sheet a Print entry shows (Art's prints; empty until they exist).")]
        [SerializeField] private Texture _print;

        [Tooltip("How the entry unlocks.")]
        [SerializeField] private DatabankUnlock _unlock;

        public DatabankEntry(string id, DatabankCategory category, string nameKey, DatabankMaker maker, string year,
            DatabankEra era, string readoutKey, string historyKey, DatabankViewKind view, GameObject model,
            string[] hiddenNodes, float framingYaw, Texture print, DatabankUnlock unlock)
        {
            _id = id;
            _category = category;
            _nameKey = nameKey;
            _maker = maker;
            _year = year;
            _era = era;
            _readoutKey = readoutKey;
            _historyKey = historyKey;
            _view = view;
            _model = model;
            _hiddenNodes = hiddenNodes ?? Array.Empty<string>();
            _framingYaw = framingYaw;
            _print = print;
            _unlock = unlock;
        }

        public string Id => _id ?? string.Empty;

        public DatabankCategory Category => _category;

        public DatabankUnlock Unlock => _unlock;

        /// <summary>The Core view of this entry for the page.</summary>
        public DatabankEntryInfo ToInfo()
        {
            return new DatabankEntryInfo(Id, _category, _nameKey, _maker, _year, _era, _readoutKey, _historyKey, _view,
                _model, _hiddenNodes, _framingYaw, _print);
        }

        /// <summary>Null when the entry is sound, else what breaks its contract.</summary>
        public string Validate()
        {
            if (string.IsNullOrEmpty(_id) || !_id.StartsWith(IdPrefix, StringComparison.Ordinal) ||
                _id.Length == IdPrefix.Length)
            {
                return $"id '{_id}' is not 'db.<name>'";
            }

            if (string.IsNullOrEmpty(_nameKey) || string.IsNullOrEmpty(_readoutKey))
            {
                return $"{_id} needs a name key and a readout key";
            }

            if (_view == DatabankViewKind.Model && _model == null)
            {
                return $"{_id} is a model entry with no model";
            }

            switch (_unlock.Kind)
            {
                case DatabankUnlockKind.StoryScan:
                case DatabankUnlockKind.FragmentOf:
                case DatabankUnlockKind.SetComplete:
                    if (string.IsNullOrEmpty(_unlock.Target))
                    {
                        return $"{_id}'s {_unlock.Kind} rule has no target";
                    }

                    break;
            }

            if (_unlock.Kind == DatabankUnlockKind.SetComplete && _unlock.NamesSeveralSets)
            {
                return $"{_id}'s SetComplete rule names several sets";
            }

            return null;
        }
    }
}
