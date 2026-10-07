using System;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Everything Bell's signals can point at, read live from the world: the cassettes (waiting until collected), then
    /// the crew log caches (until opened), then the relics (until they answer a ping). Reachability comes from each
    /// cassette's and cache's ability gate and 07's abilities; relics lie on the basin floor and are always reachable.
    /// Allocation-free.
    /// </summary>
    internal sealed class SignalTargets : ISignalTargets
    {
        private readonly CassetteField _cassettes;
        private readonly LogCacheField _logs;
        private readonly RelicField _relics;
        private readonly IRoverAbilities _abilities;

        public SignalTargets(CassetteField cassettes, LogCacheField logs, RelicField relics,
            IRoverAbilities abilities)
        {
            _cassettes = cassettes != null ? cassettes : throw new ArgumentNullException(nameof(cassettes));
            _logs = logs != null ? logs : throw new ArgumentNullException(nameof(logs));
            _relics = relics != null ? relics : throw new ArgumentNullException(nameof(relics));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
        }

        public int Count => _cassettes.Count + _logs.Count + _relics.Relics.Count;

        public SignalCandidate Get(int index)
        {
            if (index < _cassettes.Count)
            {
                CassetteDefinition cassette = _cassettes.Definition(index);
                return new SignalCandidate(BellSignalTarget.Cassette, cassette.Id, _cassettes.Site(index).Position,
                    _cassettes.IsWaiting(index), cassette.Gate.IsOpen(_abilities));
            }

            index -= _cassettes.Count;
            if (index < _logs.Count)
            {
                LogCacheDefinition cache = _logs.Definition(index);
                return new SignalCandidate(BellSignalTarget.CrewLog, cache.LogId, _logs.Position(index),
                    !_logs.IsFound(index), cache.Gate.IsOpen(_abilities));
            }

            Relic relic = _relics.Relics[index - _logs.Count];
            bool inGround = relic.State == RelicState.Buried || relic.State == RelicState.Surfacing;
            return new SignalCandidate(BellSignalTarget.Relic, relic.Definition.Id, relic.Site.Position,
                inGround && !relic.Discovered, true);
        }
    }
}
