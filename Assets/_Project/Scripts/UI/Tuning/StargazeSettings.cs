using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The HUD stepping aside while 07 stargazes, and the one-time "Look up" hint before it.</summary>
    [Serializable]
    public sealed class StargazeSettings
    {
        [Tooltip("The ambient HUD (prompts, pips, names, ticker, chips) while 07 stargazes: fade out is how slowly it "
            + "leaves as the beat begins, fade in how soon it is back when the beat ends. Only the fade times are used.")]
        [SerializeField] private RevealSettings _hud = new RevealSettings(0.8f, 2f, 0f, 1f, 1.4f, 0.6f);

        [Tooltip("The Look up hint easing in and out.")]
        [SerializeField] private RevealSettings _hint = new RevealSettings(0.6f, 0.8f, 10f, 0.9f, 1.4f, 0.6f);

        [Tooltip("Seconds after the wide shot opens (07 still under the sky) before the Look up hint appears.")]
        [Range(0f, 10f)]
        [SerializeField] private float _hintDelay = 2f;

        [Tooltip("Seconds the Look up hint stays at most; looking up, driving or the wide shot ending send it sooner.")]
        [Range(1f, 20f)]
        [SerializeField] private float _hintHoldSeconds = 6f;

        public RevealSettings Hud => _hud;

        public RevealSettings Hint => _hint;

        public float HintDelay => _hintDelay;

        public float HintHoldSeconds => _hintHoldSeconds;
    }
}
