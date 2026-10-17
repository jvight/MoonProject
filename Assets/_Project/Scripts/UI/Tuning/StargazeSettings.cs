using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The HUD stepping aside while 07 stargazes, the opening's first prompt (cutscenes O14) and
    /// the Tab chip, which show in the same low chip.
    /// </summary>
    [Serializable]
    public sealed class StargazeSettings
    {
        [Tooltip("The ambient HUD (prompts, rings, chips) while 07 stargazes: fade out is how slowly it "
            + "leaves as the beat begins, fade in how soon it is back when the beat ends. Only the fade times are used.")]
        [SerializeField] private RevealSettings _hud = new RevealSettings(0.8f, 2f, 0f, 1f, 1.4f, 0.6f);

        [Tooltip("The low chip (first prompt, Tab) easing in and out.")]
        [SerializeField] private RevealSettings _hint = new RevealSettings(0.6f, 0.8f, 10f, 0.9f, 1.4f, 0.6f);

        [Tooltip("Seconds the Tab chip stays at most; opening the hold sends it sooner.")]
        [Range(1f, 20f)]
        [SerializeField] private float _hintHoldSeconds = 6f;

        [Tooltip("Seconds 07 rests in the opening's chase frame (no look or drive input) before its first prompt, " +
            "\"Look up\" or \"Drive\", fades in (cutscenes O14: +3 s still).")]
        [Range(0f, 10f)]
        [SerializeField] private float _firstPromptRestSeconds = 3f;

        [Tooltip("Seconds the first prompt stays at most; doing its verb sends it sooner.")]
        [Range(1f, 20f)]
        [SerializeField] private float _firstPromptHoldSeconds = 6f;

        [Tooltip("Stick deflection (0..1) that counts as moving the look or driving for the first prompt; any mouse " +
            "movement counts as looking.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _firstPromptStick = 0.2f;

        public RevealSettings Hud => _hud;

        public RevealSettings Hint => _hint;

        public float HintHoldSeconds => _hintHoldSeconds;

        public float FirstPromptRestSeconds => _firstPromptRestSeconds;

        public float FirstPromptHoldSeconds => _firstPromptHoldSeconds;

        public float FirstPromptStick => _firstPromptStick;
    }
}
