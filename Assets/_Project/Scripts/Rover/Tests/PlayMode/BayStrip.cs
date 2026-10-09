using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Captures the strip that shows the Rover Bay fitted a piece (M3-14), through the game camera (the bay's view):
    /// the arm bringing the piece down, holding it just over its socket, setting it on (the landing frame) and 07
    /// turned to show it, proud. It waits for the fitting to begin (the hopper may be fed first), so it serves the
    /// stand-in captures and the real-game session alike; each frame adds a row to the report.
    /// </summary>
    public sealed class BayStrip
    {
        private const int Width = 1600;
        private const int Height = 900;

        /// <summary>The piece is this far (m) from its socket on the way, then this near to it.</summary>
        private const float OnTheWay = 1f;
        private const float OverTheSocket = 0.15f;

        /// <summary>Seconds after 07 is let go for the proud, turned frame.</summary>
        private const float ShownAfter = 0.8f;

        private readonly EventBus _events;
        private readonly RoverController _rover;
        private readonly Camera _view;
        private readonly string _folder;

        public BayStrip(EventBus events, RoverController rover, Camera view, string folder)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _rover = rover;
            _view = view;
            _folder = folder;
        }

        /// <summary>
        /// Captures <paramref name="prefix"/>-1-carried .. -4-shown while <paramref name="piece"/> (whose node is
        /// <paramref name="part"/>) is fitted, within <paramref name="timeout"/> seconds of starting to wait.
        /// </summary>
        public IEnumerator Capture(RoverKitPiece piece, Transform part, string prefix, StringBuilder report,
            float timeout)
        {
            bool installing = false;
            bool fitted = false;
            using (_events.Subscribe<RoverKitInstalling>(e => installing |= e.Piece == piece && !e.Gift))
            using (_events.Subscribe<RoverKitFitted>(e => fitted |= e.Piece == piece && !e.Gift))
            {
                Vector3 rest = part.localPosition;
                float give = Time.time + timeout;
                while (!installing)
                {
                    Assert.Less(Time.time, give, $"The bay starts fitting the {piece}.");
                    yield return null;
                }

                float started = Time.time;
                float until = started + timeout;
                int next = 0;
                float releasedAt = -1f;
                KitFit fit = _rover.Kit.Fit(piece);
                while (next < 4)
                {
                    Assert.Less(Time.time, until, $"The {piece} fitting runs its course.");
                    yield return null;
                    float above = Vector3.Dot(part.position - part.parent.TransformPoint(rest), Vector3.up);
                    if (releasedAt < 0f && fitted && !_rover.Kit.IsFitting)
                    {
                        releasedAt = Time.time;
                    }

                    float gap = Mathf.Abs(above);
                    bool take = next == 0 ? fit.IsCarried && gap <= OnTheWay
                        : next == 1 ? fit.IsCarried && gap <= OverTheSocket
                        : next == 2 ? fitted
                        : releasedAt >= 0f && Time.time >= releasedAt + ShownAfter;
                    if (take)
                    {
                        string name = $"{prefix}-{next + 1}-{FrameName(next)}";
                        FrameCapture.SavePng(_view, Width, Height, Path.Combine(_folder, name + ".png"));
                        report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                            "| {0} | {1:0.00} | {2:0.00} | {3:0} | {4:0.0} |", name, Time.time - started, above,
                            _rover.Kit.BayFitting.TurntableAngle, _rover.Heading));
                        next++;
                    }
                }
            }
        }

        /// <summary>The report's header row for <see cref="Capture"/>.</summary>
        public static void Header(StringBuilder report)
        {
            report.AppendLine("| Capture | Since the fitting began (s) | Piece above its socket (m) | Turntable (deg) "
                + "| 07 heading (deg) |");
            report.AppendLine("|---|---:|---:|---:|---:|");
        }

        private static string FrameName(int frame)
        {
            switch (frame)
            {
                case 0:
                    return "carried";
                case 1:
                    return "over-socket";
                case 2:
                    return "fitted";
                default:
                    return "shown";
            }
        }
    }
}
