using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Assembling an adapted part at the socket rack (opening v3 5.2, the SSR-3 headlamp): while 07 reads a bay whose
    /// part maker understanding has unlocked, and 07's hold covers its cost, a held E fills the bay's progress; full, it
    /// spends the cost and fits the bay's piece in its socket on 07 (<see cref="PartFitted"/>), which fills the bay.
    /// Letting go, or reading elsewhere, starts the hold again. Nothing is saved here: the rack keeps its filled bays.
    /// Allocation-free per step.
    /// </summary>
    public sealed class RackAssembly
    {
        private readonly EventBus _events;
        private readonly IRackStatus _rack;
        private readonly MakerUnderstanding _understanding;
        private readonly MaterialStock _stock;
        private readonly IReadOnlyList<RackBaySettings> _bays;
        private readonly IReadOnlyList<RackAssemblySettings> _assemblies;
        private string _bayId = string.Empty;

        public RackAssembly(EventBus events, IRackStatus rack, MakerUnderstanding understanding, MaterialStock stock,
            IReadOnlyList<RackBaySettings> bays, IReadOnlyList<RackAssemblySettings> assemblies)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _rack = rack ?? throw new ArgumentNullException(nameof(rack));
            _understanding = understanding ?? throw new ArgumentNullException(nameof(understanding));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _bays = bays ?? throw new ArgumentNullException(nameof(bays));
            _assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
        }

        /// <summary>How far the held E has come at the bay read now (0..1).</summary>
        public float Progress01 { get; private set; }

        /// <summary>
        /// True when the bay <paramref name="bayId"/> (read now) holds a part 07 may assemble: unlocked, not yet
        /// fitted; <paramref name="covered"/> says whether 07's hold has its cost.
        /// </summary>
        public bool CanAssemble(string bayId, out bool covered)
        {
            covered = false;
            if (!TryFind(bayId, out RackAssemblySettings assembly, out int bay) ||
                _rack.StateOf(bay) != RackBayState.Outline || !_understanding.IsUnlocked(assembly.AdaptedId))
            {
                return false;
            }

            covered = _stock.Has(assembly.Cost);
            return true;
        }

        /// <param name="deltaTime">Seconds since the last step.</param>
        /// <param name="readBayId">The bay 07 reads now, or null when it reads none.</param>
        /// <param name="held">E is held.</param>
        public void Step(float deltaTime, string readBayId, bool held)
        {
            if (readBayId != _bayId)
            {
                _bayId = readBayId ?? string.Empty;
                Progress01 = 0f;
            }

            if (!held || !CanAssemble(_bayId, out bool covered) || !covered)
            {
                Progress01 = 0f;
                return;
            }

            TryFind(_bayId, out RackAssemblySettings assembly, out int bay);
            Progress01 = Math.Min(1f, Progress01 + deltaTime / assembly.HoldSeconds);
            if (Progress01 < 1f || !_stock.TrySpend(assembly.Cost))
            {
                return;
            }

            Progress01 = 0f;
            _events.Publish(new PartFitted(assembly.Socket, _bays[bay].FilledBy, false, false, default, 0));
        }

        /// <summary>The assembly at bay <paramref name="bayId"/> and the bay's index on the rack.</summary>
        private bool TryFind(string bayId, out RackAssemblySettings assembly, out int bay)
        {
            assembly = null;
            bay = -1;
            if (string.IsNullOrEmpty(bayId))
            {
                return false;
            }

            for (int i = 0; i < _assemblies.Count; i++)
            {
                if (string.Equals(_assemblies[i].BayId, bayId, StringComparison.Ordinal))
                {
                    assembly = _assemblies[i];
                    break;
                }
            }

            for (int i = 0; assembly != null && i < _rack.Count; i++)
            {
                if (string.Equals(_rack.IdOf(i), bayId, StringComparison.Ordinal))
                {
                    bay = i;
                    return true;
                }
            }

            return false;
        }
    }
}
