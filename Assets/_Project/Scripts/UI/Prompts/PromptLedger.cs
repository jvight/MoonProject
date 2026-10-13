using System;
using System.Collections.Generic;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Remembers, per action, how many times its prompt was shown and how many times the player did it. A prompt is
    /// retired for good once it was shown the shared number of times or done as often as its entry says (design
    /// ruling 6: only the first few times). The Look up hint and the opening's first prompts ("Look up", "Drive"),
    /// each shown once per save, and the HUD verbs met so far
    /// (<see cref="HudVerb"/>: each HUD piece shows only once its verb is met) are remembered here too. Saved.
    /// </summary>
    internal sealed class PromptLedger
    {
        private readonly PromptSettings _settings;
        private readonly int[] _shown;
        private readonly int[] _used;
        private readonly bool[] _met = new bool[Enum.GetValues(typeof(HudVerb)).Length];

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
            return entry != null && _used[(int)kind] < entry.UsesToRetire;
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

        /// <summary>True once the player has met <paramref name="verb"/> in this save.</summary>
        public bool Met(HudVerb verb)
        {
            return _met[(int)verb];
        }

        public void RecordMet(HudVerb verb)
        {
            _met[(int)verb] = true;
        }

        public void RecordLookUpHinted()
        {
            LookUpHinted = true;
        }

        /// <summary>True once the opening's first prompt has taught "Look up" in this save.</summary>
        public bool LookTaught { get; private set; }

        /// <summary>True once the opening's first prompt has taught "Drive" in this save.</summary>
        public bool DriveTaught { get; private set; }

        /// <summary>True once the first Tab press has played the hold's boot in this save.</summary>
        public bool HoldBooted { get; private set; }

        /// <summary>True once the "Tab · Hold" chip has been shown in this save.</summary>
        public bool HoldChipShown { get; private set; }

        public void RecordHoldBooted()
        {
            HoldBooted = true;
        }

        public void RecordHoldChipShown()
        {
            HoldChipShown = true;
        }

        public void RecordLookTaught()
        {
            LookTaught = true;
        }

        public void RecordDriveTaught()
        {
            DriveTaught = true;
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

            var met = new List<string>();
            for (int i = 0; i < _met.Length; i++)
            {
                if (_met[i])
                {
                    met.Add(((HudVerb)i).ToString());
                }
            }

            return new PromptsSaveData
            {
                kinds = kinds.ToArray(), lookUpHinted = LookUpHinted, metVerbs = met.ToArray(),
                lookTaught = LookTaught, driveTaught = DriveTaught, holdBooted = HoldBooted,
                holdChipShown = HoldChipShown,
            };
        }

        /// <summary>Applies saved counts; entries naming a kind this build does not know are skipped.</summary>
        public void Restore(PromptsSaveData data)
        {
            Array.Clear(_shown, 0, _shown.Length);
            Array.Clear(_used, 0, _used.Length);
            Array.Clear(_met, 0, _met.Length);
            LookUpHinted = data != null && data.lookUpHinted;
            LookTaught = data != null && data.lookTaught;
            DriveTaught = data != null && data.driveTaught;
            HoldBooted = data != null && data.holdBooted;
            HoldChipShown = data != null && data.holdChipShown;
            if (data?.metVerbs != null)
            {
                foreach (string name in data.metVerbs)
                {
                    if (Enum.TryParse(name, false, out HudVerb verb) && (int)verb < _met.Length)
                    {
                        _met[(int)verb] = true;
                    }
                }
            }

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
