using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Art;
using Object = UnityEngine.Object;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Neutral product-shot rig for judging art against docs/VISION.md: deep indigo backdrop, soft cool key light,
    /// warm amber rim, indigo/violet trilight ambient and an optional dark dust floor that catches the key's shadow.
    /// Lights follow the camera's yaw, so every turntable view is lit the same way (like a real turntable).
    /// Creates its objects in the active scene; <see cref="Dispose"/> removes them.
    /// </summary>
    public sealed class StudioRig : IDisposable
    {
        private const float FieldOfView = 30f;
        private const float FramePadding = 1.12f;
        private const float KeyPitch = 40f;
        private const float KeyYawOffset = 225f;
        private const float KeyIntensity = 1.15f;
        private const float RimPitch = 25f;
        private const float RimYawOffset = 45f;
        private const float RimIntensity = 0.9f;
        private const float FloorRadiusScale = 20f;
        private const float FloorSmoothness = 0.1f;

        private readonly GameObject _root;
        private readonly Light _key;
        private readonly Light _rim;
        private readonly GameObject _floor;
        private readonly Material _floorMaterial;

        public StudioRig(bool withFloor)
        {
            _root = new GameObject("[StudioRig]");
            var cameraObject = new GameObject("StudioCamera");
            cameraObject.transform.SetParent(_root.transform, false);
            Camera = cameraObject.AddComponent<Camera>();
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Background;
            Camera.fieldOfView = FieldOfView;
            Camera.allowHDR = true;
            UniversalAdditionalCameraData cameraData = Camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
            cameraData.renderShadows = true;

            _key = CreateLight("KeyLight", new Color(0.80f, 0.84f, 1f), KeyIntensity, LightShadows.Soft);
            _rim = CreateLight("RimLight", Palette.Get(PaletteSwatch.WarmLamp), RimIntensity, LightShadows.None);

            Color skyTop = Palette.Get(PaletteSwatch.SkyTop);
            Color horizon = Palette.Get(PaletteSwatch.SkyHorizon);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.sun = _key;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(horizon, Palette.Get(PaletteSwatch.DustLight), 0.35f);
            RenderSettings.ambientEquatorColor = horizon * 0.8f;
            RenderSettings.ambientGroundColor = skyTop;
            RenderSettings.ambientIntensity = 1f;
            DynamicGI.UpdateEnvironment();

            if (withFloor)
            {
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null)
                {
                    throw new InvalidOperationException("URP Lit shader not found; is URP the active pipeline?");
                }

                _floorMaterial = new Material(lit) { name = "StudioFloor" };
                _floorMaterial.SetColor("_BaseColor",
                    Color.Lerp(skyTop, Palette.Get(PaletteSwatch.DustShadow), 0.6f));
                _floorMaterial.SetFloat("_Smoothness", FloorSmoothness);
                _floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _floor.name = "StudioFloor";
                Object.DestroyImmediate(_floor.GetComponent<Collider>());
                _floor.transform.SetParent(_root.transform, false);
                _floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                _floor.GetComponent<MeshRenderer>().sharedMaterial = _floorMaterial;
            }
        }

        /// <summary>Studio backdrop: deep indigo between the sky-top and horizon swatches.</summary>
        public static Color Background =>
            Color.Lerp(Palette.Get(PaletteSwatch.SkyTop), Palette.Get(PaletteSwatch.SkyHorizon), 0.3f);

        public Camera Camera { get; }

        /// <summary>
        /// Points the camera at <paramref name="bounds"/> from <paramref name="yaw"/> degrees around +Y (0 = in front
        /// of the subject, on its +Z side; 90 = on its right, +X) and <paramref name="elevation"/> degrees above the
        /// horizon, at a distance that fits the bounds (scaled by <paramref name="distanceScale"/>). Lights follow.
        /// </summary>
        public void Frame(Bounds bounds, float yaw, float elevation, float distanceScale)
        {
            float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
            float distance = radius / Mathf.Sin(FieldOfView * 0.5f * Mathf.Deg2Rad) * FramePadding * distanceScale;
            Vector3 direction = Quaternion.Euler(-elevation, yaw, 0f) * Vector3.forward;
            Transform cameraTransform = Camera.transform;
            cameraTransform.position = bounds.center + direction * distance;
            cameraTransform.rotation = Quaternion.LookRotation(bounds.center - cameraTransform.position, Vector3.up);
            Camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f) * 0.5f;
            Camera.farClipPlane = distance + radius * FloorRadiusScale * 2f;

            _key.transform.rotation = Quaternion.Euler(KeyPitch, yaw + KeyYawOffset, 0f);
            _rim.transform.rotation = Quaternion.Euler(RimPitch, yaw + RimYawOffset, 0f);

            if (_floor != null)
            {
                _floor.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                _floor.transform.localScale = Vector3.one * radius * FloorRadiusScale * 2f;
            }
        }

        public void Dispose()
        {
            Object.DestroyImmediate(_root);
            if (_floorMaterial != null)
            {
                Object.DestroyImmediate(_floorMaterial);
            }
        }

        private Light CreateLight(string name, Color color, float intensity, LightShadows shadows)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(_root.transform, false);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows;
            light.shadowStrength = 0.75f;
            return light;
        }
    }
}
