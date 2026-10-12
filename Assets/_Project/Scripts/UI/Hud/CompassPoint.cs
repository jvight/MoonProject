using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The eight compass points, north along +Z and clockwise, and their string keys ("ui.compass.ne").</summary>
    internal static class CompassPoint
    {
        public const int Count = 8;
        private const float Sector = 360f / Count;

        /// <summary>The compass point from <paramref name="from"/> towards <paramref name="to"/>: 0 north, 1 north-east, ...</summary>
        public static int Of(Vector3 from, Vector3 to)
        {
            float degrees = Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg;
            if (degrees < 0f)
            {
                degrees += 360f;
            }

            return Mathf.RoundToInt(degrees / Sector) % Count;
        }

        public static string Key(int point)
        {
            switch (point)
            {
                case 1:
                    return "ui.compass.ne";
                case 2:
                    return "ui.compass.e";
                case 3:
                    return "ui.compass.se";
                case 4:
                    return "ui.compass.s";
                case 5:
                    return "ui.compass.sw";
                case 6:
                    return "ui.compass.w";
                case 7:
                    return "ui.compass.nw";
                default:
                    return "ui.compass.n";
            }
        }
    }
}
