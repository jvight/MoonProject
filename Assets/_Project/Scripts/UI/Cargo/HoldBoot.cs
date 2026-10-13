using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The timeline of the first Tab press (pure, EditMode-tested): one line of 07's system text types in, the slot
    /// outlines light left to right, and the items 07 starts with slide into their slots one after another. The
    /// screen reads the counts and progress each frame; this holds only time.
    /// </summary>
    internal sealed class HoldBoot
    {
        private readonly CargoScreenSettings _settings;
        private int _slots;
        private int _items;
        private float _time;

        public HoldBoot(CargoScreenSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>True from <see cref="Start"/> until the last item has arrived.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>How long the whole boot takes for the current slots and items.</summary>
        public float TotalSeconds => _items == 0
            ? ItemStart(0)
            : ItemStart(_items - 1) + _settings.BootSlideSeconds;

        /// <summary>0..1 how much of the system line is typed.</summary>
        public float TypedFraction => Mathf.Clamp01(_time / _settings.BootTypeSeconds);

        /// <summary>How many slot outlines are lit.</summary>
        public int LitCount => Mathf.Clamp(Mathf.FloorToInt((_time - _settings.BootTypeSeconds) /
                                                            _settings.BootLightStepSeconds) + 1, 0, _slots);

        /// <summary>How many items have finished sliding in.</summary>
        public int ArrivedCount
        {
            get
            {
                int arrived = 0;
                for (int k = 0; k < _items; k++)
                {
                    arrived += ItemProgress(k) >= 1f ? 1 : 0;
                }

                return arrived;
            }
        }

        /// <summary>Begins the boot for a hold of <paramref name="slots"/> slots carrying <paramref name="items"/> stacks.</summary>
        public void Start(int slots, int items)
        {
            if (slots <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slots), slots, "A hold has slots.");
            }

            _slots = slots;
            _items = Mathf.Clamp(items, 0, slots);
            _time = 0f;
            IsRunning = true;
        }

        /// <summary>0..1 how far item <paramref name="k"/> (in slot order among the filled ones) has slid in.</summary>
        public float ItemProgress(int k)
        {
            return Mathf.Clamp01((_time - ItemStart(k)) / _settings.BootSlideSeconds);
        }

        /// <summary>Ends the boot early (the screen closed).</summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        public void Step(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            _time += deltaTime;
            if (_time >= TotalSeconds)
            {
                _time = TotalSeconds;
                IsRunning = false;
            }
        }

        private float ItemStart(int k)
        {
            return _settings.BootTypeSeconds + _slots * _settings.BootLightStepSeconds +
                   k * _settings.BootItemStepSeconds;
        }
    }
}
