using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The HUD's battery arc (docs/features/M3-20 "How 07 shows it"): how it eases in and out. It exists once the
    /// charge has first dropped below the reveal share, and shows only while the cells are not full and the beam
    /// rests.
    /// </summary>
    [Serializable]
    public sealed class BatteryArcSettings
    {
        [Tooltip("The arc easing in as the cells start to empty, and out once full or while the beam works.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(1.2f, 0.9f, 4f, 0.97f, 1.2f, 0.7f);

        public RevealSettings Reveal => _reveal;
    }
}
