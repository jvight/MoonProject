using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using MoonProject.World;

namespace MoonProject.Art.PlayModeTests
{
    /// <summary>
    /// The look spike's fixed vista (look_max_vista.json): image size, the day C and night A clock times, where 07 and
    /// the built solar array stand, the still poses and the camera move. Heights in the file are metres above the
    /// ground under each point, so the poses survive terrain tweaks; <see cref="ResolvedPosesJson"/> writes them out
    /// absolute, in Unity and Unreal coordinates, so the same shots can be set up in the other engine.
    /// </summary>
    [Serializable]
    public sealed class LookMaxVista
    {
        private const string FilePath = "Assets/_Project/Scripts/Art/Tests/PlayMode/look_max_vista.json";

        public int width;
        public int height;
        public float dayClock;
        public float nightClock;
        public int settleFrames;
        public int timingFrames;
        public Mark rover;
        public Mark array;
        public Pose[] poses;
        public string[] night;
        public Move move;

        public static LookMaxVista Load()
        {
            var vista = JsonUtility.FromJson<LookMaxVista>(File.ReadAllText(FilePath));
            if (vista.poses == null || vista.poses.Length == 0 || vista.move == null)
            {
                throw new InvalidOperationException($"{FilePath} needs poses and a move.");
            }

            return vista;
        }

        /// <summary>Points <paramref name="camera"/> along <paramref name="pose"/>.</summary>
        public void Aim(Camera camera, Pose pose, Func<float, float, float> ground)
        {
            Place(camera, Absolute(pose.eye, ground), Absolute(pose.look, ground), pose.fov);
        }

        /// <summary>Points <paramref name="camera"/> at <paramref name="t"/> (0..1) along the move, eased in and out.</summary>
        public void AimMove(Camera camera, float t, Func<float, float, float> ground)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            Vector3 eye = Vector3.Lerp(Absolute(move.fromEye, ground), Absolute(move.toEye, ground), s);
            Vector3 look = Vector3.Lerp(Absolute(move.fromLook, ground), Absolute(move.toLook, ground), s);
            Place(camera, eye, look, move.fov);
        }

        /// <summary>Every pose, the move's ends, the marks and the sun at both clock times, resolved and absolute.</summary>
        public string ResolvedPosesJson(SkyCycleSystem sky, Func<float, float, float> ground)
        {
            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine("  \"note\": \"Unity: metres, Y up, left-handed. Unreal: centimetres, X = Unity Z, " +
                "Y = Unity X, Z = Unity Y. Camera rotation is look-at (eye -> look); fov is vertical, degrees, " +
                $"image {width}x{height}.\",");
            json.AppendLine($"  \"image\": [{width}, {height}],");
            AppendSun(json, "day", sky, dayClock);
            AppendSun(json, "night", sky, nightClock);
            json.AppendLine($"  \"rover07\": {Point(rover.Position(ground))}, \"rover07Yaw\": {F(rover.yaw)},");
            json.AppendLine($"  \"solarArray\": {Point(array.Position(ground))}, \"solarArrayYaw\": {F(array.yaw)},");
            json.AppendLine("  \"poses\": [");
            for (int i = 0; i < poses.Length; i++)
            {
                Pose pose = poses[i];
                json.AppendLine($"    {{\"name\": \"{pose.name}\", \"eye\": {Point(Absolute(pose.eye, ground))}, " +
                    $"\"look\": {Point(Absolute(pose.look, ground))}, \"fovVertical\": {F(pose.fov)}}}" +
                    (i < poses.Length - 1 ? "," : ""));
            }

            json.AppendLine("  ],");
            json.AppendLine($"  \"move\": {{\"name\": \"{move.name}\", \"seconds\": {F(move.seconds)}, " +
                $"\"fps\": {move.fps}, \"fovVertical\": {F(move.fov)}, \"easing\": \"smoothstep\",");
            json.AppendLine($"    \"fromEye\": {Point(Absolute(move.fromEye, ground))}, " +
                $"\"fromLook\": {Point(Absolute(move.fromLook, ground))},");
            json.AppendLine($"    \"toEye\": {Point(Absolute(move.toEye, ground))}, " +
                $"\"toLook\": {Point(Absolute(move.toLook, ground))}}}");
            json.AppendLine("}");
            return json.ToString();
        }

        private static void AppendSun(StringBuilder json, string name, SkyCycleSystem sky, float clock)
        {
            sky.SetClockTime(clock);
            Vector3 sun = sky.SunDirection;
            json.AppendLine($"  \"{name}\": {{\"clockSeconds\": {F(clock)}, \"phase\": \"{sky.Phase}\", " +
                $"\"sunElevation\": {F(Mathf.Asin(sun.y) * Mathf.Rad2Deg)}, " +
                $"\"sunBearingFromUnityZ\": {F(Mathf.Atan2(sun.x, sun.z) * Mathf.Rad2Deg)}, " +
                $"\"towardSun\": {Point(sun)}}},");
        }

        private static string Point(Vector3 p)
        {
            return $"{{\"unity\": [{F(p.x)}, {F(p.y)}, {F(p.z)}], " +
                $"\"unreal\": [{F(p.z * 100f)}, {F(p.x * 100f)}, {F(p.y * 100f)}]}}";
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static Vector3 Absolute(float[] xyz, Func<float, float, float> ground)
        {
            return new Vector3(xyz[0], ground(xyz[0], xyz[2]) + xyz[1], xyz[2]);
        }

        private static void Place(Camera camera, Vector3 eye, Vector3 look, float fov)
        {
            camera.fieldOfView = fov;
            camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
        }

        /// <summary>Where something stands: x, z on the ground, turned <see cref="yaw"/> degrees about up.</summary>
        [Serializable]
        public sealed class Mark
        {
            public float x;
            public float z;
            public float yaw;

            public Vector3 Position(Func<float, float, float> ground)
            {
                return new Vector3(x, ground(x, z), z);
            }
        }

        /// <summary>A still: eye and look-at point (x, height above ground, z) and vertical field of view.</summary>
        [Serializable]
        public sealed class Pose
        {
            public string name;
            public float[] eye;
            public float[] look;
            public float fov;
        }

        /// <summary>The camera move: from one eye/look pair to another over <see cref="seconds"/>.</summary>
        [Serializable]
        public sealed class Move
        {
            public string name;
            public float seconds;
            public int fps;
            public float fov;
            public float[] fromEye;
            public float[] fromLook;
            public float[] toEye;
            public float[] toLook;
        }
    }
}
