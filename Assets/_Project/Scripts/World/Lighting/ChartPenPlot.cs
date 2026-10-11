using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Where Kenji's chart pen draws a place on Ines's chart (docs/features/M3-07): the paper shows the basin round
    /// home, its middle over home, its edges <c>radius</c> metres out along world X (across) and Z (up the paper).
    /// Places beyond the edge are drawn at it. Plain logic.
    /// </summary>
    public static class ChartPenPlot
    {
        /// <summary>The place at <paramref name="place"/> on the paper, 0..1 across and up.</summary>
        public static Vector2 OnPaper(Vector3 place, Vector3 home, float radius)
        {
            float span = Mathf.Max(radius, Mathf.Epsilon) * 2f;
            return new Vector2(Mathf.Clamp01(0.5f + (place.x - home.x) / span),
                Mathf.Clamp01(0.5f + (place.z - home.z) / span));
        }
    }
}
