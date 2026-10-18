using UnityEngine;
using UnityEngine.UIElements;
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

        /// <summary>Centre of the device's frame in the 1152x768 reference frame.</summary>
        public static Vector2 CenterOf(int device)
        {
            return FaceLayout.DeviceFrame(device).center;
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

        /// <summary>
        /// The device the selection moves to from <paramref name="device"/> in <paramref name="direction"/>, or -1
        /// when that edge leads to the hold (down from the lower frames) or nowhere. The frames sit at the four
        /// corners, so the keys follow the screen: across to the frame beside, down or up to the one below or above.
        /// </summary>
        public static int Step(int device, NavigationMoveEvent.Direction direction)
        {
            switch (direction)
            {
                case NavigationMoveEvent.Direction.Right:
                    return device == Scan ? Cutter : device == SocketOne ? SocketTwo : -1;
                case NavigationMoveEvent.Direction.Left:
                    return device == Cutter ? Scan : device == SocketTwo ? SocketOne : -1;
                case NavigationMoveEvent.Direction.Down:
                    return device == Scan ? SocketOne : device == Cutter ? SocketTwo : -1;
                case NavigationMoveEvent.Direction.Up:
                    return device == SocketOne ? Scan : device == SocketTwo ? Cutter : -1;
                default:
                    return -1;
            }
        }
    }
}
