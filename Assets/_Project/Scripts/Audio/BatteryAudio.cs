using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// 07's battery heard (docs/features/M3-20), always soft and in key, never an alarm: each cell a charger fills (or
    /// a new cell pack brings) plays the next note of D E F# A B, then up an octave, at 07; the Low warning plays one
    /// falling two-note (A to F#); an empty battery a soft power-down sigh as the crawl begins; a leap the battery
    /// can't pay for the coils' quiet fizzle under the belly; each flank lamp waking in the opening a quiet tick
    /// (<see cref="FlankLampLit"/>). A loaded game's battery is shown silently. The charging
    /// hum is the pad's, following whichever charger fills the cells (<see cref="FeelAudio"/>).
    /// Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BatteryAudio : MonoBehaviour
    {
        private const int SubscriptionCount = 5;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private AudioDirector _director;
        private IRoverState _rover;
        private CueHandle _cell;
        private CueHandle _low;
        private CueHandle _sigh;
        private CueHandle _fizzle;
        private CueHandle _tick;
        private int _cellNotes;
        private int _fullCells = -1;

        /// <summary>The cell notes played so far (tests).</summary>
        internal int CellsPlayed { get; private set; }

        internal void Initialize(GameContext context, AudioDirector director)
        {
            _cell = director.Resolve(AudioCueIds.BatteryCell);
            _low = director.Resolve(AudioCueIds.BatteryLow);
            _sigh = director.Resolve(AudioCueIds.BatterySigh);
            _fizzle = director.Resolve(AudioCueIds.LeapFizzle);
            _tick = director.Resolve(AudioCueIds.LampTick);
            if (!_cell.IsValid || !_low.IsValid || !_sigh.IsValid || !_fizzle.IsValid || !_tick.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _rover = context.Get<IRoverState>();
            _cellNotes = director.Library.GetCue(_cell).ClipCount;
            _subscriptions[0] = context.Events.Subscribe<BatteryChanged>(OnBatteryChanged);
            _subscriptions[1] = context.Events.Subscribe<BatteryWarned>(OnWarned);
            _subscriptions[2] = context.Events.Subscribe<LowPowerChanged>(OnLowPowerChanged);
            _subscriptions[3] = context.Events.Subscribe<RoverLeapRefused>(OnLeapRefused);
            _subscriptions[4] = context.Events.Subscribe<FlankLampLit>(OnLampLit);
        }

        /// <summary>A rising cell plays its note: the cell's place in the run, D5 for the first.</summary>
        private void OnBatteryChanged(BatteryChanged changed)
        {
            int previous = _fullCells;
            _fullCells = changed.FullCells;
            if (changed.Restored || !changed.Rising || previous < 0 || changed.FullCells <= 0)
            {
                return;
            }

            int note = Mathf.Clamp(changed.FullCells - 1, 0, _cellNotes - 1);
            _director.PlayVariantAt(_cell, note, _rover.Position);
            CellsPlayed++;
        }

        private void OnWarned(BatteryWarned warned)
        {
            if (warned.Warning == BatteryWarning.Low)
            {
                _director.PlayAt(_low, _rover.Position);
            }
        }

        private void OnLowPowerChanged(LowPowerChanged lowPower)
        {
            if (lowPower.LowPower && !lowPower.Restored)
            {
                _director.PlayAt(_sigh, _rover.Position);
            }
        }

        private void OnLeapRefused(RoverLeapRefused refused)
        {
            _director.PlayAt(_fizzle, refused.Position);
        }

        private void OnLampLit(FlankLampLit lit)
        {
            _director.PlayAt(_tick, lit.Position);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
                _subscriptions[i] = null;
            }
        }
    }
}
