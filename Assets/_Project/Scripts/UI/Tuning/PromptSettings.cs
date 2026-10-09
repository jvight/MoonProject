using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core.Input;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Context prompts (design ruling 6): a glyph and one word near a usable thing, only the first few times, one at a
    /// time, then never again.
    /// </summary>
    [Serializable]
    public sealed class PromptSettings
    {
        [Tooltip("The prompt chip easing in and out.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.35f, 0.3f, 10f, 0.88f, 1.6f, 0.55f);

        [Tooltip("Teachable actions. The tower upgrade is taught by its own panel, so it has no entry.")]
        [SerializeField] private PromptEntry[] _entries =
        {
            new PromptEntry(InteractionKind.Ping, RoverAction.Ping, 6f, 1.9f),
            new PromptEntry(InteractionKind.Excavate, RoverAction.Excavate, 0.5f, 0.9f),
            new PromptEntry(InteractionKind.Tether, RoverAction.Tether, 0.4f, 0.9f),
            new PromptEntry(InteractionKind.Deposit, RoverAction.Tether, 0.25f, 1.4f),
            new PromptEntry(InteractionKind.Reel, RoverAction.Winch, 2.5f, 0.9f),
            new PromptEntry(InteractionKind.Repair, RoverAction.Excavate, 0.5f, 0.8f),
            new PromptEntry(InteractionKind.Tune, RoverAction.Excavate, 0.6f, 1.8f, 1),
            new PromptEntry(InteractionKind.Restore, RoverAction.Excavate, 0.4f, 0.9f),
            new PromptEntry(InteractionKind.Hop, RoverAction.Excavate, 0.8f, 1.7f),
            new PromptEntry(InteractionKind.Salvage, RoverAction.Excavate, 0.4f, 0.6f),
            new PromptEntry(InteractionKind.Stow, RoverAction.Tether, 0.4f, 0.9f),
        };

        [Tooltip("Seconds after 07 starts waking before any prompt may appear (the opening belongs to the moon).")]
        [Range(0f, 30f)]
        [SerializeField] private float _startDelay = 9f;

        [Tooltip("Seconds one prompt may stay up before it bows out on its own.")]
        [Range(2f, 30f)]
        [SerializeField] private float _maxShowSeconds = 7f;

        [Tooltip("Seconds before the same prompt may come back after it went away.")]
        [Range(0f, 120f)]
        [SerializeField] private float _repeatCooldown = 25f;

        [Tooltip("Times a prompt is shown before it is retired for good (saved).")]
        [Range(1, 10)]
        [SerializeField] private int _showingsToRetire = 3;

        [Tooltip("Half-life (s) of the prompt following its world point on screen: it floats, never jitters.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _followHalfLife = 0.08f;

        [Tooltip("Minimum distance (px at 1080p) between the prompt and the screen edge.")]
        [Range(0f, 300f)]
        [SerializeField] private float _screenMargin = 72f;

        public RevealSettings Reveal => _reveal;

        public PromptEntry[] Entries => _entries;

        public float StartDelay => _startDelay;

        public float MaxShowSeconds => _maxShowSeconds;

        public float RepeatCooldown => _repeatCooldown;

        public int ShowingsToRetire => _showingsToRetire;

        public float FollowHalfLife => _followHalfLife;

        public float ScreenMargin => _screenMargin;

        /// <summary>The entry teaching <paramref name="kind"/>, or null when that kind has no prompt.</summary>
        public PromptEntry Find(InteractionKind kind)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i] != null && _entries[i].Kind == kind)
                {
                    return _entries[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Appends the code's default entry for every kind the table lacks (a kind added after the tuning asset was
        /// created), keeping every hand-tuned entry as it is. Returns how many were added.
        /// </summary>
        internal int AddMissingDefaults()
        {
            var defaults = new PromptSettings();
            var merged = new List<PromptEntry>(_entries);
            foreach (PromptEntry entry in defaults._entries)
            {
                if (Find(entry.Kind) == null)
                {
                    merged.Add(entry);
                }
            }

            int added = merged.Count - _entries.Length;
            _entries = merged.ToArray();
            return added;
        }

        /// <summary>Null when the table is usable, else the first problem.</summary>
        public string Validate()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                PromptEntry entry = _entries[i];
                if (entry == null || entry.Kind == InteractionKind.None)
                {
                    return $"prompt entry {i} has no interaction kind";
                }

                for (int j = 0; j < i; j++)
                {
                    if (_entries[j].Kind == entry.Kind)
                    {
                        return $"prompt kind {entry.Kind} appears twice";
                    }
                }
            }

            return null;
        }
    }
}
