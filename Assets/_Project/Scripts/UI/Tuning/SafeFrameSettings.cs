using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// How far the UI spreads on a wide screen (docs/features/M3-24 part A). The panel scales by the screen's height,
    /// so on a screen wider than these shapes the extra width is left to the world and the UI stays in a centred
    /// frame of this shape, where it keeps its place against the scene the title and the HUD were composed over.
    /// </summary>
    [Serializable]
    public sealed class SafeFrameSettings
    {
        [Tooltip("Widest shape (width / height) the HUD spreads to: 21:9 is 2.33. A wider screen keeps its HUD in a " +
                 "centred frame of this shape.")]
        [Range(1.5f, 3.6f)]
        [SerializeField] private float _hudAspect = 2.3333f;

        [Tooltip("Widest shape (width / height) the title and the pause menu spread to: the title is composed for " +
                 "16:9 (1.78), so its wordmark keeps its place against Earth on a wider screen.")]
        [Range(1.5f, 3.6f)]
        [SerializeField] private float _menuAspect = 1.7778f;

        public float HudAspect => _hudAspect;

        public float MenuAspect => _menuAspect;

        /// <summary>Null when usable, else the first problem.</summary>
        public string Validate()
        {
            return _hudAspect < 1f || _menuAspect < 1f ? "a safe frame's aspect is below 1:1" : null;
        }
    }
}
