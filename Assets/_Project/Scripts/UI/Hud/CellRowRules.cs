using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// When the cell row shows and how bright it is (pure, EditMode-tested; docs/design/first-hour-v2 6.4): once the
    /// charge has first dropped below the reveal share it shows while the cells are not full, while a charger fills
    /// them and for a few seconds after full, and dims while the beam works instead of hiding.
    /// </summary>
    internal sealed class CellRowRules
    {
        private readonly CellRowSettings _settings;
        private float _sinceFull;

        public CellRowRules(CellRowSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sinceFull = settings.FullHoldSeconds;
            Dim = 1f;
        }

        /// <summary>The row should be on screen.</summary>
        public bool Shown { get; private set; }

        /// <summary>The brightness the row eases to: 1, or the dim share while the beam works.</summary>
        public float Dim { get; private set; }

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="revealed">The row has revealed itself in this game.</param>
        /// <param name="open">False while something else needs the screen (pause, the map, stargazing).</param>
        /// <param name="full">The cells are full.</param>
        /// <param name="charging">A charger is filling the cells.</param>
        /// <param name="working">The beam is at work.</param>
        public void Tick(float deltaTime, bool revealed, bool open, bool full, bool charging, bool working)
        {
            if (!full || charging)
            {
                _sinceFull = 0f;
            }
            else
            {
                _sinceFull = Mathf.Min(_sinceFull + deltaTime, _settings.FullHoldSeconds);
            }

            bool needed = !full || charging || _sinceFull < _settings.FullHoldSeconds;
            Shown = open && revealed && needed;
            Dim = working ? _settings.DimShare : 1f;
        }

        /// <summary>
        /// The cell being drawn: the last lit one while it is part-full (draining or filling), else none.
        /// </summary>
        public static int InUse(int cells, float charge)
        {
            if (cells <= 0 || charge <= 0f || charge >= cells)
            {
                return -1;
            }

            float whole = Mathf.Floor(charge);
            return charge > whole ? (int)whole : -1;
        }
    }
}
