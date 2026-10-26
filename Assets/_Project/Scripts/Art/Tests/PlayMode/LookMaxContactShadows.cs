using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MoonProject.Art.PlayModeTests
{
    /// <summary>
    /// Look spike only: screen-space main-light shadows with contact shadows, injected into one camera's frames at
    /// runtime (no renderer feature asset is touched). Before the opaques, the cascaded shadow and a short ray toward
    /// the sun through the depth buffer are resolved into _ScreenSpaceShadowmapTexture and the shaders are switched to
    /// read it (every lit shader in the project compiles _MAIN_LIGHT_SHADOWS_SCREEN); before the transparents the
    /// cascade keywords come back, as URP's own screen-space shadows do.
    /// </summary>
    public sealed class LookMaxContactShadows : IDisposable
    {
        private const string ShaderName = "Hidden/MoonProject/Art/LookMaxContactShadows";
        private const float Length = 0.9f;
        private const float Thickness = 0.35f;
        private const float MaxDistance = 80f;

        private static readonly int ShadowTextureId = Shader.PropertyToID("_ScreenSpaceShadowmapTexture");
        private static readonly GlobalKeyword MainLightShadows = GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS");
        private static readonly GlobalKeyword MainLightCascades = GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_CASCADE");
        private static readonly GlobalKeyword MainLightScreen = GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_SCREEN");

        private readonly Camera _camera;
        private readonly Material _material;
        private readonly ResolvePass _resolve;
        private readonly RestorePass _restore;

        public LookMaxContactShadows(Camera camera)
        {
            _camera = camera;
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"{ShaderName} is missing.");
            }

            _material = new Material(shader);
            _material.SetFloat("_ContactLength", Length);
            _material.SetFloat("_ContactThickness", Thickness);
            _material.SetFloat("_ContactMaxDistance", MaxDistance);
            _resolve = new ResolvePass(_material)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPrePasses + 1,
            };
            _resolve.ConfigureInput(ScriptableRenderPassInput.Depth);
            _restore = new RestorePass { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            Object.Destroy(_material);
        }

        private void OnBeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera)
            {
                return;
            }

            ScriptableRenderer renderer = camera.GetUniversalAdditionalCameraData().scriptableRenderer;
            renderer.EnqueuePass(_resolve);
            renderer.EnqueuePass(_restore);
        }

        private sealed class ResolvePass : ScriptableRenderPass
        {
            private readonly Material _material;

            public ResolvePass(Material material)
            {
                _material = material;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                RenderTextureDescriptor desc = frameData.Get<UniversalCameraData>().cameraTargetDescriptor;
                desc.depthStencilFormat = GraphicsFormat.None;
                desc.msaaSamples = 1;
                desc.graphicsFormat = GraphicsFormat.R8_UNorm;
                TextureHandle target = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc,
                    "_ScreenSpaceShadowmapTexture", true);
                using (IRasterRenderGraphBuilder builder =
                       renderGraph.AddRasterRenderPass("LookMax contact shadows", out PassData data))
                {
                    data.Material = _material;
                    builder.SetRenderAttachment(target, 0, AccessFlags.Write);
                    builder.UseTexture(frameData.Get<UniversalResourceData>().cameraDepthTexture);
                    builder.AllowGlobalStateModification(true);
                    builder.SetGlobalTextureAfterPass(target, ShadowTextureId);
                    builder.SetRenderFunc((PassData pass, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, new Vector4(1f, 1f, 0f, 0f), pass.Material, 0);
                        context.cmd.SetKeyword(MainLightShadows, false);
                        context.cmd.SetKeyword(MainLightCascades, false);
                        context.cmd.SetKeyword(MainLightScreen, true);
                    });
                }
            }

            private sealed class PassData
            {
                public Material Material;
            }
        }

        private sealed class RestorePass : ScriptableRenderPass
        {
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                using (IRasterRenderGraphBuilder builder =
                       renderGraph.AddRasterRenderPass("LookMax shadow keywords back", out PassData data))
                {
                    UniversalShadowData shadows = frameData.Get<UniversalShadowData>();
                    data.Cascades = shadows.supportsMainLightShadows && shadows.mainLightShadowCascadesCount > 1;
                    data.Single = shadows.supportsMainLightShadows && shadows.mainLightShadowCascadesCount == 1;
                    builder.SetRenderAttachment(frameData.Get<UniversalResourceData>().activeColorTexture, 0,
                        AccessFlags.Write);
                    builder.AllowGlobalStateModification(true);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((PassData pass, RasterGraphContext context) =>
                    {
                        context.cmd.SetKeyword(MainLightScreen, false);
                        context.cmd.SetKeyword(MainLightShadows, pass.Single);
                        context.cmd.SetKeyword(MainLightCascades, pass.Cascades);
                    });
                }
            }

            private sealed class PassData
            {
                public bool Cascades;
                public bool Single;
            }
        }
    }
}
