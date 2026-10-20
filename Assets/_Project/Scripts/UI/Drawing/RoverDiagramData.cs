using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// 07 v3's equipment socket anchor positions, wheel centres, and the 25 parsed SVG icons (<see cref="UiIconShape"/>)
    /// extracted at build time from <c>Assets/_Project/Generated/Art/R1/Models/Rover/RoverModel.prefab</c> and
    /// <c>Assets/_Project/UI/Icons/</c> by <c>UI/Rover Diagram</c>, and the face's own art from
    /// <c>Assets/_Project/Art/UI/Face/</c>: 07's three-quarter line drawing, where its lines sit in the image, the point
    /// on it each device's leader ends at, and the face's parsed icons, for <see cref="FaceInventoryView"/>.
    /// </summary>
    public sealed class RoverDiagramData : ScriptableObject
    {
        public const int SocketCount = 12;
        public const int WheelCount = 4;

        /// <summary>The face icons (<c>Art/UI/Face/&lt;id&gt;.svg</c>) the screen draws, in ordinal order.</summary>
        public static readonly string[] FaceIconIds =
        {
            "icon_cutter", "icon_empty_socket", "icon_feed_unit", "icon_ice_core_tube", "icon_lens", "icon_metal_plates",
            "icon_spare_igniter", "icon_warning", "icon_wiring_spool",
        };

        [Tooltip("Root-local socket positions on 07 v3, indexed by Core.RoverSocket (0..11).")]
        [SerializeField] private Vector3[] _sockets = Array.Empty<Vector3>();

        [Tooltip("Root-local wheel centres on 07 v3 (Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR).")]
        [SerializeField] private Vector3[] _wheels = Array.Empty<Vector3>();

        [Tooltip("The 25 parsed 64x64 vector icons (item_*.svg and kit_*.svg) drawn by FaceInventoryView.")]
        [SerializeField] private UiIconShape[] _icons = Array.Empty<UiIconShape>();

        [Tooltip("The face's parsed icons (FaceIconIds, scaled from their 256 viewBox to 64) drawn by FaceInventoryView.")]
        [SerializeField] private UiIconShape[] _faceIcons = Array.Empty<UiIconShape>();

        [Tooltip("07's three-quarter line drawing on the face (Art/UI/Face/rover_07_line_3q.png).")]
        [SerializeField] private Texture2D _faceArt;

        [Tooltip("Where the drawing's lines sit inside the image, as a UV rect with a top-left origin.")]
        [SerializeField] private Rect _faceArtContent;

        [Tooltip("Per FaceDevices index, the UV (top-left origin) on the drawing that device's leader ends at.")]
        [SerializeField] private Vector2[] _faceAnchors = Array.Empty<Vector2>();

        public int Count => _sockets != null ? _sockets.Length : 0;

        public int IconCount => _icons != null ? _icons.Length : 0;

        /// <summary>07's three-quarter line drawing on the face.</summary>
        public Texture2D FaceArt => _faceArt;

        /// <summary>Where the drawing's lines sit inside <see cref="FaceArt"/> (UV, top-left origin).</summary>
        public Rect FaceArtContent => _faceArtContent;

        /// <summary>The UV (top-left origin) on <see cref="FaceArt"/> where device <paramref name="device"/>'s leader ends.</summary>
        public Vector2 FaceAnchor(int device)
        {
            return _faceAnchors[device];
        }

        /// <summary>Contract transform name on <c>RoverModel.prefab</c> for <paramref name="socket"/>.</summary>
        public static string SocketNodeName(RoverSocket socket)
        {
            return socket == RoverSocket.Panniers ? "BinSocket_L" : socket + "Socket";
        }

        /// <summary>Contract transform name on <c>RoverModel.prefab</c> for wheel <paramref name="index"/> (0..3).</summary>
        public static string WheelNodeName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Wheel_FL";
                case 1:
                    return "Wheel_FR";
                case 2:
                    return "Wheel_RL";
                case 3:
                    return "Wheel_RR";
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Wheel index must be 0..3.");
            }
        }

        /// <summary>Root-local position (m) of <paramref name="socket"/> on 07 v3.</summary>
        public Vector3 SocketPosition(RoverSocket socket)
        {
            int index = (int)socket;
            if (_sockets == null || index < 0 || index >= _sockets.Length)
            {
                return Vector3.zero;
            }

            return _sockets[index];
        }

        /// <summary>Root-local centre (m) of wheel <paramref name="index"/> (0=FL, 1=FR, 2=RL, 3=RR) on 07 v3.</summary>
        public Vector3 WheelPosition(int index)
        {
            if (_wheels == null || index < 0 || index >= _wheels.Length)
            {
                return Vector3.zero;
            }

            return _wheels[index];
        }

        /// <summary>
        /// Looks up the parsed 64x64 vector icon for <paramref name="id"/>. Throws <see cref="InvalidOperationException"/>
        /// loudly when no icon matches.
        /// </summary>
        public UiIconShape GetIcon(string id)
        {
            return Array.IndexOf(FaceIconIds, id) >= 0 ? UiIconSet.FindInArray(_faceIcons, id)
                : UiIconSet.FindInArray(_icons, id);
        }

        public UiIconShape GetIcon(SalvageMaterial material)
        {
            return GetIcon(UiIconSet.IconIdOf(material));
        }

        public UiIconShape GetPartIcon(string partId)
        {
            return GetIcon(UiIconSet.IconIdOfPart(partId));
        }

        internal void SetSockets(Vector3[] sockets)
        {
            _sockets = sockets ?? throw new ArgumentNullException(nameof(sockets));
        }

        internal void SetWheels(Vector3[] wheels)
        {
            _wheels = wheels ?? throw new ArgumentNullException(nameof(wheels));
        }

        internal void SetIcons(UiIconShape[] icons)
        {
            _icons = icons ?? throw new ArgumentNullException(nameof(icons));
        }

        internal void SetFaceArt(Texture2D art, Rect content, Vector2[] anchors, UiIconShape[] icons)
        {
            _faceArt = art;
            _faceArtContent = content;
            _faceAnchors = anchors ?? throw new ArgumentNullException(nameof(anchors));
            _faceIcons = icons ?? throw new ArgumentNullException(nameof(icons));
        }

        /// <summary>
        /// Null when all 12 socket positions, 4 wheel centres, the 25 icons and the face's drawing, anchors and icons
        /// are present and populated.
        /// </summary>
        public string Validate()
        {
            string faceProblem = ValidateFace();
            if (faceProblem != null)
            {
                return "face art " + faceProblem;
            }

            if (_sockets == null || _sockets.Length != SocketCount)
            {
                return $"needs {SocketCount} socket positions (has {_sockets?.Length ?? 0})";
            }

            if (_wheels == null || _wheels.Length != WheelCount)
            {
                return $"needs {WheelCount} wheel positions (has {_wheels?.Length ?? 0})";
            }

            bool anySocketNonZero = false;
            for (int i = 0; i < _sockets.Length; i++)
            {
                if (_sockets[i].sqrMagnitude > 1e-6f)
                {
                    anySocketNonZero = true;
                    break;
                }
            }

            if (!anySocketNonZero)
            {
                return "has uninitialised (all-zero) socket positions";
            }

            for (int i = 0; i < _wheels.Length; i++)
            {
                if (_wheels[i].sqrMagnitude <= 1e-6f)
                {
                    return $"has uninitialised wheel position at index {i} ({WheelNodeName(i)})";
                }
            }

            string iconProblem = UiIconSet.ValidateArray(_icons);
            return iconProblem != null ? "icons: " + iconProblem : null;
        }

        private string ValidateFace()
        {
            if (_faceArt == null)
            {
                return "has no line drawing";
            }

            if (_faceArt.width != _faceArt.height)
            {
                return $"has a {_faceArt.width}x{_faceArt.height} line drawing; the face places a square one";
            }

            if (_faceArtContent.width <= 0f || _faceArtContent.height <= 0f || _faceArtContent.xMin < 0f ||
                _faceArtContent.yMin < 0f || _faceArtContent.xMax > 1f || _faceArtContent.yMax > 1f)
            {
                return $"has a content rect {_faceArtContent} outside the image";
            }

            if (_faceAnchors == null || _faceAnchors.Length != FaceDevices.Count)
            {
                return $"needs {FaceDevices.Count} device anchors (has {_faceAnchors?.Length ?? 0})";
            }

            for (int i = 0; i < _faceAnchors.Length; i++)
            {
                if (!_faceArtContent.Contains(_faceAnchors[i]))
                {
                    return $"has device {i}'s anchor {_faceAnchors[i]} off the drawing";
                }
            }

            if (_faceIcons == null || _faceIcons.Length != FaceIconIds.Length)
            {
                return $"needs {FaceIconIds.Length} icons (has {_faceIcons?.Length ?? 0})";
            }

            for (int i = 0; i < FaceIconIds.Length; i++)
            {
                if (!string.Equals(_faceIcons[i].Id, FaceIconIds[i], StringComparison.Ordinal))
                {
                    return $"has icon '{_faceIcons[i].Id}' where '{FaceIconIds[i]}' belongs";
                }

                string problem = _faceIcons[i].Validate();
                if (problem != null)
                {
                    return problem;
                }
            }

            return null;
        }
    }
}
