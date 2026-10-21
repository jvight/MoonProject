using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How scanning teaches 07 each maker's standards (docs/design/maker-understanding.md): one track per maker whose
    /// adapted parts exist so far.
    /// </summary>
    [Serializable]
    public sealed class MakerUnderstandingSettings
    {
        /// <summary>The SSR-3 headlamp, adapted from 06's lamp (Kessler–Arai level 1).</summary>
        public const string Ssr3Headlamp = MakerTrack.AdaptedPrefix + "ssr3_lamp";

        [Tooltip("One track per maker: the points each level needs and what it unlocks. A maker with no track gains " +
                 "no meter until its first adapted part exists.")]
        [SerializeField] private MakerTrack[] _tracks =
        {
            new MakerTrack(DatabankMaker.KesslerArai, new[] { 2 }, new[] { Ssr3Headlamp }),
        };

        public MakerUnderstandingSettings()
        {
        }

        public MakerUnderstandingSettings(params MakerTrack[] tracks)
        {
            _tracks = tracks;
        }

        public IReadOnlyList<MakerTrack> Tracks => _tracks;

        /// <summary>True when some maker's level unlocks <paramref name="adaptedId"/>.</summary>
        public bool Unlocks(string adaptedId)
        {
            for (int i = 0; _tracks != null && i < _tracks.Length; i++)
            {
                for (int level = 1; _tracks[i] != null && level <= _tracks[i].Levels; level++)
                {
                    if (string.Equals(_tracks[i].Adapted(level), adaptedId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Null when every track can work and no maker has two, else the first problem.</summary>
        public string Validate()
        {
            if (_tracks == null)
            {
                return "maker understanding has no tracks";
            }

            for (int i = 0; i < _tracks.Length; i++)
            {
                string problem = _tracks[i] != null ? _tracks[i].Validate() : $"understanding track {i} is empty";
                if (problem != null)
                {
                    return problem;
                }

                for (int j = 0; j < i; j++)
                {
                    if (_tracks[j].Maker == _tracks[i].Maker)
                    {
                        return $"{_tracks[i].Maker} has two understanding tracks";
                    }
                }
            }

            return null;
        }
    }
}
