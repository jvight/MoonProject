using System;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The materials chip (docs/features/M3-13). No permanent HUD: when 07's metal, wiring or optics change, a small
    /// chip with the three material icons and counts eases in, each count eases to its new amount, the material that
    /// grew glows softly (<see cref="MaterialTally"/>), and the chip lingers a few seconds and eases out. It stays
    /// while pinned (at a station or a mast, where the stock matters). Changes before the UI's first frame are a loaded
    /// save: they are taken as they are, with no chip.
    /// </summary>
    internal sealed class MaterialsChip
    {
        private readonly MaterialsChipSettings _settings;
        private readonly Reveal _reveal;
        private readonly MaterialTally _tally;
        private readonly MaterialSlots _slots;
        private float _linger;
        private bool _pinned;
        private bool _armed;

        public MaterialsChip(UiLayout layout, MaterialsChipSettings settings, IntText numbers)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _reveal = new Reveal(layout.MaterialsChip, settings.Reveal);
            _reveal.Snap(false);
            _tally = new MaterialTally(settings);
            _slots = new MaterialSlots(layout.MaterialsChipItems, numbers, true);
            new ShadowPainter(layout.MaterialsChipShadow);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The slots (tests and captures).</summary>
        public MaterialSlots Slots => _slots;

        /// <summary>The number on <paramref name="material"/>'s slot right now.</summary>
        public int Shown(SalvageMaterial material)
        {
            return _tally.Shown(material);
        }

        /// <summary>0..1 how strongly <paramref name="material"/> glows right now.</summary>
        public float Glow(SalvageMaterial material)
        {
            return _tally.Glow(material);
        }

        /// <summary>The stock changed: before the first frame (a load) it is taken as it is; after, shown.</summary>
        public void Change(int metal, int wiring, int optics)
        {
            if (!_armed)
            {
                _tally.Snap(metal, wiring, optics);
                Write();
                return;
            }

            if (_tally.Apply(metal, wiring, optics))
            {
                _linger = 0f;
                _reveal.Show();
            }
        }

        public void SetPinned(bool pinned)
        {
            if (pinned && !_pinned)
            {
                _reveal.Show();
            }

            _pinned = pinned;
            if (pinned)
            {
                _linger = 0f;
            }
        }

        /// <param name="deltaTime">Unscaled seconds since the last tick.</param>
        public void Tick(float deltaTime)
        {
            _armed = true;
            if (_tally.Step(deltaTime))
            {
                WriteCounts();
            }

            WriteGlows();
            if (!_pinned && _reveal.Target && _tally.IsSettled && !_tally.IsGlowing)
            {
                _linger += deltaTime;
                if (_linger >= _settings.LingerSeconds)
                {
                    _reveal.Hide();
                }
            }

            _reveal.Tick(deltaTime);
        }

        private void Write()
        {
            WriteCounts();
            WriteGlows();
        }

        private void WriteCounts()
        {
            for (int i = 0; i < Materials.Count; i++)
            {
                SalvageMaterial material = Materials.At(i);
                _slots.SetCount(material, _tally.Shown(material));
            }
        }

        private void WriteGlows()
        {
            for (int i = 0; i < Materials.Count; i++)
            {
                SalvageMaterial material = Materials.At(i);
                _slots.SetGlow(material, _tally.Glow(material), _settings.PulseScale, _settings.GlowOpacity);
            }
        }
    }
}
