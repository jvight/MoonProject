using System;
using System.Collections.Generic;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Remembers, per action, how many times its prompt was shown and how many times the player did it. A prompt is
    /// retired for good once either count reaches its limit (design ruling 6: only the first few times). Saved.
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

        /// <summary>True while the prompt for <paramref name="kind"/> still has something to teach.</summary>
        public bool ShouldTeach(InteractionKind kind)
        {
            return kind != InteractionKind.None && _shown[(int)kind] < _settings.ShowingsToRetire &&
                   _used[(int)kind] < _settings.UsesToRetire;
        }

        public void RecordShown(InteractionKind kind)
        {
            if (kind != InteractionKind.None)
            {
                _shown[(int)kind]++;
            }
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

            return new PromptsSaveData { kinds = kinds.ToArray() };
        }

        /// <summary>Applies saved counts; entries naming a kind this build does not know are skipped.</summary>
        public void Restore(PromptsSaveData data)
        {
            Array.Clear(_shown, 0, _shown.Length);
            Array.Clear(_used, 0, _used.Length);
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
