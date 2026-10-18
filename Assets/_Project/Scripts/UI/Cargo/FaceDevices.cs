using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The four devices 07's socket display shows around its line drawing (docs/design/opening-v2.md §10): the scan
    /// lens, the cutter and the two empty sockets. They are fixed, so the display reads each one's state from
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

        // The face icon in each device's status disc (RoverDiagramData.FaceIconIds).
        private static readonly string[] IconIds =
        {
            "icon_lens", "icon_cutter", "icon_empty_socket", "icon_empty_socket",
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

        /// <summary>The face icon drawn in device <paramref name="device"/>'s status disc.</summary>
        public static string IconId(int device)
        {
            return IconIds[device];
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

        /// <summary>True for a state in which the device does not work (a fault, jammed, receive-only): amber, marked.</summary>
        public static bool IsFaulty(RoverSystemState state)
        {
            return state != RoverSystemState.Ok && state != RoverSystemState.Empty;
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
        /// when that edge leads to the hold (left from the left frames) or nowhere. The cutter and the lens stand
        /// above 07, the two sockets below it, so the keys follow the screen: across to the frame beside, down or up
        /// to the one below or above.
        /// </summary>
        public static int Step(int device, NavigationMoveEvent.Direction direction)
        {
            switch (direction)
            {
                case NavigationMoveEvent.Direction.Right:
                    return device == Cutter ? Scan : device == SocketOne ? SocketTwo : -1;
                case NavigationMoveEvent.Direction.Left:
                    return device == Scan ? Cutter : device == SocketTwo ? SocketOne : -1;
                case NavigationMoveEvent.Direction.Down:
                    return device == Cutter ? SocketOne : device == Scan ? SocketTwo : -1;
                case NavigationMoveEvent.Direction.Up:
                    return device == SocketOne ? Cutter : device == SocketTwo ? Scan : -1;
                default:
                    return -1;
            }
        }
    }
}
