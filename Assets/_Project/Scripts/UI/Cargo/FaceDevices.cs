using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The four devices 07's socket display shows beside its line art (docs/design/opening-v2.md §10): the scan lens,
    /// the cutter and the two empty sockets. They are fixed, so the display reads each one's state from
    /// <see cref="IRoverSystems"/>. Pure lookups, EditMode-tested.
    /// </summary>
    internal static class FaceDevices
    {
        public const int Count = 4;
        public const int Scan = 0;
        public const int Cutter = 1;
        public const int SocketOne = 2;
        public const int SocketTwo = 3;

        private static readonly RoverSystem[] Systems =
        {
            RoverSystem.Scan, RoverSystem.Cutter, RoverSystem.Sockets, RoverSystem.Sockets,
        };

        // The line-art mount each device's leader runs to, and the icon its bubble borrows.
        private static readonly RoverSocket[] Mounts =
        {
            RoverSocket.Tether, RoverSocket.Cutter, RoverSocket.Flanks, RoverSocket.Belly,
        };

        private static readonly Vector2[] Centers =
        {
            new Vector2(948f, 130f),
            new Vector2(960f, 228f),
            new Vector2(1064f, 504f),
            new Vector2(908f, 560f),
        };

        private static readonly string[] NameKeys =
        {
            "ui.device.scan.name", "ui.device.cutter.name", "ui.device.socket1.name", "ui.device.socket2.name",
        };

        /// <summary>The system whose state device <paramref name="device"/> shows.</summary>
        public static RoverSystem SystemOf(int device)
        {
            return Systems[device];
        }

        /// <summary>The socket whose icon and line-art mount stand for device <paramref name="device"/>.</summary>
        public static RoverSocket MountOf(int device)
        {
            return Mounts[device];
        }

        /// <summary>Centre of the device's bubble in the 1152x768 reference frame.</summary>
        public static Vector2 CenterOf(int device)
        {
            return Centers[device];
        }

        public static string NameKey(int device)
        {
            return NameKeys[device];
        }

        /// <summary>True when the cutter socket offers the spare igniter: <paramref name="canFitIgniter"/> from the rover.</summary>
        public static bool OffersIgniter(int device, bool canFitIgniter)
        {
            return device == Cutter && canFitIgniter;
        }

        /// <summary>The one short line the tooltip says about the device in its current state.</summary>
        public static string DescriptionKey(int device, RoverSystemState state, bool canFitIgniter)
        {
            switch (device)
            {
                case Scan:
                    return "ui.device.scan.ok";
                case Cutter:
                    return state == RoverSystemState.Ok ? "ui.device.cutter.ok"
                        : canFitIgniter ? "ui.device.cutter.fit" : "ui.device.cutter.fault";
                default:
                    return "ui.device.socket.empty";
            }
        }

        /// <summary>The device whose bubble centre is nearest to <paramref name="y"/> in the reference frame.</summary>
        public static int NearestTo(float y)
        {
            int best = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                float distance = Mathf.Abs(Centers[i].y - y);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }
    }
}
