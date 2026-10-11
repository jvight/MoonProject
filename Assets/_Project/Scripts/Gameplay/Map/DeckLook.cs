using System;
using UnityEngine;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// 07's time on the lander deck as plain logic (docs/features/M3-07 steps 1 and 4, amendment 4; cutscenes §6):
    /// the lift settles, the lookout's sweep blinks the places in view, and then the view eases into the crew's map.
    /// On the first ride the hatch swings open as the sweep ends and the lookout breathes until the player looks at
    /// the hatch or 07 rests; on later rides the hatch is already open and the map follows the sweep. Driving leaves
    /// at any point. While <see cref="HoldsDeck"/>, look input belongs to the lookout and the map, not to the lift.
    /// </summary>
    public sealed class DeckLook
    {
        private readonly MapTuning _tuning;
        private float _breathStarted;
        private bool _released;

        public DeckLook(MapTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        public DeckLookPhase Phase { get; private set; }

        /// <summary>The hatch swung open once (saved): later rides open straight onto the map.</summary>
        public bool HatchOpen { get; private set; }

        /// <summary>The current look is the save's first (the view drifts to the paper's edge after the ink).</summary>
        public bool FirstLook { get; private set; }

        /// <summary>The deck's input belongs to the map: the lift waits for <see cref="DeckLookStep.Released"/>.</summary>
        public bool HoldsDeck => Phase != DeckLookPhase.Off;

        public void Restore(bool hatchOpen)
        {
            HatchOpen = hatchOpen;
            FirstLook = false;
            Phase = DeckLookPhase.Off;
            _released = false;
        }

        public DeckLookStep Step(DeckLookInput input)
        {
            if (!input.AtDeck)
            {
                DeckLookStep left = Phase == DeckLookPhase.Off ? DeckLookStep.None : End();
                _released = false;
                return left;
            }

            switch (Phase)
            {
                case DeckLookPhase.Off:
                    if (_released)
                    {
                        return DeckLookStep.None;
                    }

                    Phase = DeckLookPhase.Sweeping;
                    return DeckLookStep.SweepStarted;
                case DeckLookPhase.Sweeping:
                    if (input.Leave)
                    {
                        return End();
                    }

                    if (!input.SweepDone)
                    {
                        return DeckLookStep.None;
                    }

                    if (HatchOpen)
                    {
                        Phase = DeckLookPhase.Looking;
                        return DeckLookStep.LookStarted;
                    }

                    HatchOpen = true;
                    FirstLook = true;
                    Phase = DeckLookPhase.Breathing;
                    _breathStarted = input.Now;
                    return DeckLookStep.HatchOpened;
                case DeckLookPhase.Breathing:
                    if (input.Leave)
                    {
                        return End();
                    }

                    float rested = Mathf.Min(input.StillSeconds, input.Now - _breathStarted);
                    if (!input.LookingAtHatch && rested < _tuning.BreatheSeconds)
                    {
                        return DeckLookStep.None;
                    }

                    Phase = DeckLookPhase.Looking;
                    return DeckLookStep.LookStarted;
                case DeckLookPhase.Looking:
                    return input.Leave ? End() : DeckLookStep.None;
                default:
                    throw new InvalidOperationException($"Unknown deck look phase {Phase}.");
            }
        }

        private DeckLookStep End()
        {
            bool looking = Phase == DeckLookPhase.Looking;
            Phase = DeckLookPhase.Off;
            FirstLook = false;
            _released = true;
            return looking ? DeckLookStep.LookEnded : DeckLookStep.Released;
        }
    }
}
