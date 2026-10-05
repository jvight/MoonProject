using System;

namespace MoonProject.Editor.Automation
{
    /// <summary>One named camera placement of a <see cref="CameraPoseSet"/>.</summary>
    [Serializable]
    public sealed class CameraPose
    {
        public string name;
        public float[] position = Array.Empty<float>();
        public float[] lookAt = Array.Empty<float>();
        public float[] euler = Array.Empty<float>();
        public float fov;
    }
}
