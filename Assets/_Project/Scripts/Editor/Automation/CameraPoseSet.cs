using System;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// JSON file read by <see cref="CaptureAutomation.CaptureScene"/>. Field names are the JSON keys:
    /// <code>
    /// { "width": 1600, "height": 900, "postProcessing": true,
    ///   "poses": [
    ///     { "name": "overview", "position": [0, 120, -260], "lookAt": [0, 0, 0], "fov": 50 },
    ///     { "name": "rover-low", "position": [4, 1.2, -6], "euler": [8, -30, 0] } ] }
    /// </code>
    /// A pose aims with <c>lookAt</c> (world point) when given, else with <c>euler</c> (degrees). Size defaults to
    /// 1600x900, fov to 50.
    /// </summary>
    [Serializable]
    public sealed class CameraPoseSet
    {
        public int width;
        public int height;
        public bool postProcessing = true;
        public CameraPose[] poses = Array.Empty<CameraPose>();
    }
}
