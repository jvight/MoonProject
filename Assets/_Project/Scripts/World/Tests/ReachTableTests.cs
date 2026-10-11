using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The battery's reach table (docs/features/M3-20 §4, "the 300 m rule"): every world anchor (site hearts, friend
    /// spots, the canyon's beats, the relay masts) with its metres to home's dock and to the nearest relay pad, written
    /// to Logs/world-captures/reach.md for the Director's check against the masts lit by then.
    /// </summary>
    public sealed class ReachTableTests
    {
        private const string ReportFolder = "Logs/world-captures";
        private const string ReportName = "reach.md";

        /// <summary>
        /// Metres a charger may stand from any place on 0.5's golden path (the longest crawl ~130 s).
        /// </summary>
        private const float ChargerReach = 300f;

        [Test]
        public void TheReachTable_ListsEveryPlace_AgainstHomeAndTheNearestMastPad()
        {
            var settings = new SurfaceSettings();
            var surface = new MoonSurface(settings, WorldSettings.DefaultSeed);
            var anchors = new WorldAnchors(surface, settings.Canyon, new RelaySettings());
            var home = new Vector3(0f, 0f, 0f);
            var report = new StringBuilder();
            report.AppendLine("# Reach table (docs/features/M3-20 §4)");
            report.AppendLine();
            report.AppendLine("| Place | To home (m) | Nearest mast pad | To it (m) | Nearest charger, all lit (m) |");
            report.AppendLine("|---|---:|---|---:|---:|");
            int masts = 0;
            for (int i = 0; i < anchors.Count; i++)
            {
                masts += anchors.Get(i).Id.StartsWith(WorldAnchorIds.RelayPrefix, System.StringComparison.Ordinal)
                    ? 1
                    : 0;
            }

            Assert.Greater(masts, 0, "the basin has relay masts");
            for (int i = 0; i < anchors.Count; i++)
            {
                WorldAnchor place = anchors.Get(i);
                float toHome = Flat(place.Position - home);
                string nearest = string.Empty;
                float toMast = float.PositiveInfinity;
                for (int m = 0; m < anchors.Count; m++)
                {
                    WorldAnchor mast = anchors.Get(m);
                    if (m == i || !mast.Id.StartsWith(WorldAnchorIds.RelayPrefix, System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    float distance = Flat(place.Position - mast.Position);
                    if (distance < toMast)
                    {
                        toMast = distance;
                        nearest = mast.Id;
                    }
                }

                float charger = Mathf.Min(toHome, toMast);
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0} | {1:F0} | {2} | {3:F0} | {4:F0}{5} |", place.Id, toHome, nearest, toMast, charger,
                    charger > ChargerReach ? " (over 300)" : string.Empty));
            }

            Directory.CreateDirectory(ReportFolder);
            File.WriteAllText(Path.Combine(ReportFolder, ReportName), report.ToString());
            Debug.Log("[reach] " + report);
        }

        private static float Flat(Vector3 offset)
        {
            return new Vector2(offset.x, offset.z).magnitude;
        }
    }
}
