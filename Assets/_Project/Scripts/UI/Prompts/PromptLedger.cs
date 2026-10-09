using System;
using System.Collections.Generic;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Remembers, per action, how many times its prompt was shown and how many times the player did it. A prompt is
    /// retired for good once it was shown the shared number of times or done as often as its entry says (design
    /// ruling 6: only the first few times). The Look up hint, shown once per save, is remembered here too. Saved.
    /// </summary>
    internal sealed class PromptLedger
    {
        private readonly PromptSettings _settings;
        private readonly int[] _shown;
        private readonly int[] _used;

        public PromptLedger(PromptSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            int count = 0;
            foreach (InteractionKind kind in Enum.GetValues(typeof(InteractionKind)))
            {
                count = Math.Max(count, (int)kind + 1);
            }

            _shown = new int[count];
            _used = new int[count];
        }

        public int Shown(InteractionKind kind)
        {
            return _shown[(int)kind];
        }

        public int Used(InteractionKind kind)
        {
            return _used[(int)kind];
        }

        /// <summary>
        /// True while the prompt for <paramref name="kind"/> still has something to teach (false for a kind without a
        /// prompt entry).
        /// </summary>
        public bool ShouldTeach(InteractionKind kind)
        {
            PromptEntry entry = _settings.Find(kind);
            return entry != null && _shown[(int)kind] < _settings.ShowingsToRetire &&
                   _used[(int)kind] < entry.UsesToRetire;
        }

        public void RecordShown(InteractionKind kind)
        {
            if (kind != InteractionKind.None)
            {
                _shown[(int)kind]++;
            }
        }

        /// <summary>True once the Look up hint has been shown in this save.</summary>
        public bool LookUpHinted { get; private set; }

        public void RecordLookUpHinted()
        {
            LookUpHinted = true;
        }

        public void RecordUsed(InteractionKind kind)
        {
            if (kind != InteractionKind.None)
            {
                _used[(int)kind]++;
            }
        }

        public PromptsSaveData Capture()
        {
            var kinds = new List<PromptCountData>();
            for (int i = 1; i < _shown.Length; i++)
            {
                if (_shown[i] > 0 || _used[i] > 0)
                {
                    kinds.Add(new PromptCountData
                    {
                        kind = ((InteractionKind)i).ToString(), shown = _shown[i], used = _used[i],
                    });
                }
            }

            return new PromptsSaveData { kinds = kinds.ToArray(), lookUpHinted = LookUpHinted };
        }

        /// <summary>Applies saved counts; entries naming a kind this build does not know are skipped.</summary>
        public void Restore(PromptsSaveData data)
        {
            Array.Clear(_shown, 0, _shown.Length);
            Array.Clear(_used, 0, _used.Length);
            LookUpHinted = data != null && data.lookUpHinted;
            if (data?.kinds == null)
            {
                return;
            }

            foreach (PromptCountData entry in data.kinds)
            {
                if (entry != null && Enum.TryParse(entry.kind, false, out InteractionKind kind) &&
                    kind != InteractionKind.None && (int)kind < _shown.Length)
                {
                    _shown[(int)kind] = Math.Max(0, entry.shown);
                    _used[(int)kind] = Math.Max(0, entry.used);
                }
            }
        }
    }
}
