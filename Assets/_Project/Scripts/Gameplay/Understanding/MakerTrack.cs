using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One maker's understanding (docs/design/maker-understanding.md §2): the points each level needs and the adapted
    /// blueprint each level unlocks.
    /// </summary>
    [Serializable]
    public sealed class MakerTrack
    {
        /// <summary>Every adapted blueprint id starts with this ("adapted.ssr3_lamp").</summary>
        public const string AdaptedPrefix = "adapted.";

        [Tooltip("Whose parts these are.")]
        [SerializeField] private DatabankMaker _maker;

        [Tooltip("The points each level needs, rising (Kessler–Arai: 2, then 4, 7, 10 as their adapted parts exist).")]
        [SerializeField] private int[] _thresholds = Array.Empty<int>();

        [Tooltip("The adapted blueprint each level unlocks, one per threshold: 'adapted.<id>', saved, never renamed.")]
        [SerializeField] private string[] _adapted = Array.Empty<string>();

        public MakerTrack()
        {
        }

        public MakerTrack(DatabankMaker maker, int[] thresholds, string[] adapted)
        {
            _maker = maker;
            _thresholds = thresholds ?? Array.Empty<int>();
            _adapted = adapted ?? Array.Empty<string>();
        }

        public DatabankMaker Maker => _maker;

        /// <summary>The levels this track has.</summary>
        public int Levels => _thresholds.Length;

        /// <summary>The points level <paramref name="level"/> (1..<see cref="Levels"/>) needs.</summary>
        public int Threshold(int level)
        {
            return _thresholds[level - 1];
        }

        /// <summary>The adapted blueprint level <paramref name="level"/> (1..<see cref="Levels"/>) unlocks.</summary>
        public string Adapted(int level)
        {
            return _adapted[level - 1];
        }

        /// <summary>The level <paramref name="points"/> reach (0 below the first threshold).</summary>
        public int LevelAt(int points)
        {
            int level = 0;
            while (level < _thresholds.Length && points >= _thresholds[level])
            {
                level++;
            }

            return level;
        }

        /// <summary>Null when the track can work, else the first problem.</summary>
        public string Validate()
        {
            if (_maker == DatabankMaker.None)
            {
                return "an understanding track has no maker";
            }

            if (_thresholds == null || _thresholds.Length == 0 || _adapted == null ||
                _adapted.Length != _thresholds.Length)
            {
                return $"{_maker}'s understanding needs one adapted blueprint per threshold";
            }

            for (int i = 0; i < _thresholds.Length; i++)
            {
                if (_thresholds[i] < 1 || (i > 0 && _thresholds[i] <= _thresholds[i - 1]))
                {
                    return $"{_maker}'s understanding thresholds must start at 1 or more and rise";
                }

                if (string.IsNullOrEmpty(_adapted[i]) ||
                    !_adapted[i].StartsWith(AdaptedPrefix, StringComparison.Ordinal) ||
                    _adapted[i].Length == AdaptedPrefix.Length)
                {
                    return $"{_maker}'s level {i + 1} blueprint '{_adapted[i]}' is not 'adapted.<id>'";
                }
            }

            return null;
        }
    }
}
