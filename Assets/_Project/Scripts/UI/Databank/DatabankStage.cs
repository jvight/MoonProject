using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MoonProject.UI
{
    /// <summary>
    /// The databank's model stage (docs/design/databank.md §4, §5): one entry's model turning in front of its own
    /// camera and lights, far from the world, filmed into a render texture the page shows. The model is a render-only
    /// copy of the entry's prefab (meshes and materials, none of its behaviours), with the entry's hidden nodes left
    /// out so the page matches the world. It draws in the amber scan wireframe, faint until its set is whole, or in its
    /// real materials in full colour. The camera renders only while the page shows it.
    /// </summary>
    internal sealed class DatabankStage : IDisposable
    {
        private const int DepthBits = 16;

        // An unnamed layer nothing in the world uses: the stage camera films only the model, and the stage lights light
        // only the model, wherever either stands.
        private const int StageLayer = 31;
        private const int StageMask = 1 << StageLayer;
        private const float NearClip = 0.05f;
        private const float FarClipMargin = 2f;
        private const float ExtentFloor = 0.05f;
        private static readonly int BlueprintModeId = Shader.PropertyToID("_BlueprintMode");
        private static readonly int StageFadeId = Shader.PropertyToID("_StageFade");

        private readonly DatabankSettings _settings;
        private readonly GameObject _root;
        private readonly Transform _pivot;
        private readonly Camera _camera;
        private readonly Light _key;
        private readonly Light _fill;
        private readonly RenderTexture _texture;
        private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();
        private readonly List<Material[]> _colours = new List<Material[]>();
        private readonly List<Material[]> _wires = new List<Material[]>();
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private GameObject _model;
        private bool _colour;
        private float _fade = 1f;

        public DatabankStage(DatabankSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (settings.WireMaterial == null)
            {
                throw new ArgumentException("The databank stage needs its wireframe material.", nameof(settings));
            }

            Vector2Int size = settings.StageResolution;
            _texture = new RenderTexture(size.x, size.y, DepthBits, RenderTextureFormat.ARGB32)
            {
                name = "DatabankStage",
            };
            _root = new GameObject("DatabankStage");
            _root.transform.position = settings.StagePosition;
            _pivot = new GameObject("Pivot").transform;
            _pivot.SetParent(_root.transform, false);
            _camera = new GameObject("StageCamera").AddComponent<Camera>();
            _camera.transform.SetParent(_root.transform, false);
            _camera.cullingMask = StageMask;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.fieldOfView = settings.StageFieldOfView;
            _camera.aspect = size.x / (float)size.y;
            _camera.nearClipPlane = NearClip;
            _camera.targetTexture = _texture;
            _camera.enabled = false;
            _key = MakeLight("StageKey", settings.KeyColour, settings.KeyIntensity, settings.KeyRotation);
            _fill = MakeLight("StageFill", settings.FillColour, settings.FillIntensity, settings.FillRotation);
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            RenderPipelineManager.endCameraRendering += OnEndCamera;
        }

        /// <summary>What the page shows in the model's rect.</summary>
        public RenderTexture Texture => _texture;

        /// <summary>True while a model stands on the stage.</summary>
        public bool HasModel => _model != null;

        /// <summary>
        /// Puts <paramref name="prefab"/> on the stage (a render-only copy without <paramref name="hiddenNodes"/>),
        /// framed to fit; null clears the stage.
        /// </summary>
        public void Show(GameObject prefab, IReadOnlyList<string> hiddenNodes)
        {
            Clear();
            if (prefab == null)
            {
                return;
            }

            _model = new GameObject(prefab.name) { layer = StageLayer };
            _model.transform.SetParent(_pivot, false);
            _model.transform.localRotation = prefab.transform.localRotation;
            _model.transform.localScale = prefab.transform.localScale;
            CopyRenderers(prefab.transform, _model.transform, hiddenNodes);
            Frame();
            Apply();
        }

        /// <summary>
        /// Turns the model to <paramref name="yaw"/> and tips it by <paramref name="pitch"/> (degrees).
        /// </summary>
        public void Turn(float yaw, float pitch)
        {
            _pivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        /// <summary>
        /// Real materials (<paramref name="colour"/>) or the wireframe at <paramref name="fade"/> (0..1).
        /// </summary>
        public void SetLook(bool colour, float fade)
        {
            if (colour == _colour && Mathf.Approximately(fade, _fade))
            {
                return;
            }

            _colour = colour;
            _fade = fade;
            Apply();
        }

        /// <summary>The camera films only while the page shows the model.</summary>
        public void SetFilming(bool filming)
        {
            _camera.enabled = filming && _model != null;
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            RenderPipelineManager.endCameraRendering -= OnEndCamera;
            Object.Destroy(_root);
            _texture.Release();
            Object.Destroy(_texture);
        }

        private void Clear()
        {
            if (_model != null)
            {
                Object.Destroy(_model);
                _model = null;
            }

            _renderers.Clear();
            _colours.Clear();
            _wires.Clear();
            _camera.enabled = false;
        }

        private void CopyRenderers(Transform source, Transform copy, IReadOnlyList<string> hiddenNodes)
        {
            var filter = source.GetComponent<MeshFilter>();
            var renderer = source.GetComponent<MeshRenderer>();
            if (filter != null && renderer != null && filter.sharedMesh != null && renderer.enabled)
            {
                copy.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                MeshRenderer drawn = copy.gameObject.AddComponent<MeshRenderer>();
                drawn.shadowCastingMode = ShadowCastingMode.Off;
                drawn.receiveShadows = false;
                Material[] colours = renderer.sharedMaterials;
                var wires = new Material[colours.Length];
                for (int i = 0; i < wires.Length; i++)
                {
                    wires[i] = _settings.WireMaterial;
                }

                _renderers.Add(drawn);
                _colours.Add(colours);
                _wires.Add(wires);
            }

            for (int i = 0; i < source.childCount; i++)
            {
                Transform child = source.GetChild(i);
                if (!child.gameObject.activeSelf || IsHidden(child.name, hiddenNodes))
                {
                    continue;
                }

                Transform childCopy = new GameObject(child.name) { layer = StageLayer }.transform;
                childCopy.SetParent(copy, false);
                childCopy.SetLocalPositionAndRotation(child.localPosition, child.localRotation);
                childCopy.localScale = child.localScale;
                CopyRenderers(child, childCopy, hiddenNodes);
            }
        }

        private static bool IsHidden(string name, IReadOnlyList<string> hiddenNodes)
        {
            if (hiddenNodes == null)
            {
                return false;
            }

            for (int i = 0; i < hiddenNodes.Count; i++)
            {
                if (string.Equals(hiddenNodes[i], name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Centres the model on the pivot and backs the camera off until its bounds fit the frame.</summary>
        private void Frame()
        {
            _pivot.localRotation = Quaternion.identity;
            if (_renderers.Count == 0)
            {
                return;
            }

            Bounds bounds = _renderers[0].bounds;
            for (int i = 1; i < _renderers.Count; i++)
            {
                bounds.Encapsulate(_renderers[i].bounds);
            }

            _model.transform.position -= bounds.center - _pivot.position;
            // Fit the height as it stands and the width as it turns (the circle its footprint sweeps), each to its own
            // half-angle of the wide frame.
            Vector3 extents = Vector3.Max(bounds.extents, Vector3.one * ExtentFloor);
            float sweep = Mathf.Sqrt(extents.x * extents.x + extents.z * extents.z);
            float tanHigh = Mathf.Tan(_settings.StageFieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanWide = tanHigh * _camera.aspect;
            float distance = Mathf.Max(extents.y / tanHigh, sweep / tanWide) * _settings.StageFrameMargin + sweep;
            _camera.transform.localPosition = new Vector3(0f, 0f, -distance);
            _camera.transform.localRotation = Quaternion.identity;
            _camera.farClipPlane = distance + sweep * FarClipMargin;
        }

        private void Apply()
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                MeshRenderer renderer = _renderers[i];
                renderer.sharedMaterials = _colour ? _colours[i] : _wires[i];
                if (_colour)
                {
                    renderer.SetPropertyBlock(null);
                    continue;
                }

                _block.Clear();
                _block.SetFloat(BlueprintModeId, 1f);
                _block.SetFloat(StageFadeId, _fade);
                renderer.SetPropertyBlock(_block);
            }
        }

        private Light MakeLight(string name, Color colour, float intensity, Quaternion rotation)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(_root.transform, false);
            light.transform.localRotation = rotation;
            light.type = LightType.Directional;
            light.color = colour;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.cullingMask = StageMask;
            light.enabled = false;
            return light;
        }

        private void OnBeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                _key.enabled = true;
                _fill.enabled = true;
            }
        }

        private void OnEndCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                _key.enabled = false;
                _fill.enabled = false;
            }
        }
    }
}
