using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The crew's lab on screen (docs/features/M3-19): the HUD steps aside inside; the lab's place line and its first
    /// few prompts (Turn, Look, Down, and Place facing the keepsake shelf) show low and quiet instead.
    /// </summary>
    [Serializable]
    public sealed class LabHudSettings
    {
        [Tooltip("The lab's prompts row easing in after the ride and out as 07 leaves.")]
        [SerializeField] private RevealSettings _prompts = new RevealSettings(0.6f, 0.5f, 10f, 0.9f, 1.4f, 0.6f);

        [Tooltip("One prompt chip easing in and out within the row.")]
        [SerializeField] private RevealSettings _chip = new RevealSettings(0.4f, 0.4f, 6f, 0.9f, 1.6f, 0.7f);

        [Tooltip("The lab's place line (its name and its one job) easing in and out.")]
        [SerializeField] private RevealSettings _place = new RevealSettings(0.8f, 1.2f, 10f, 0.95f, 1.4f, 0.7f);

        [Tooltip("Seconds the lab's place line stays the first time 07 is up in the lab.")]
        [Range(1f, 12f)]
        [SerializeField] private float _placeSeconds = 5f;

        [Tooltip("Seconds after the lab opens before its first-time prompts show (the turntable turns first).")]
        [Range(0f, 6f)]
        [SerializeField] private float _promptDelay = 1.5f;

        [Tooltip("Stick push (0..1) that counts as the player having turned, looked or gone down once: that prompt is " +
                 "learned and retires.")]
        [Range(0.2f, 0.95f)]
        [SerializeField] private float _learnedPush = 0.6f;

        public RevealSettings Prompts => _prompts;

        public RevealSettings Chip => _chip;

        public RevealSettings Place => _place;

        public float PlaceSeconds => _placeSeconds;

        public float PromptDelay => _promptDelay;

        public float LearnedPush => _learnedPush;
    }
}
