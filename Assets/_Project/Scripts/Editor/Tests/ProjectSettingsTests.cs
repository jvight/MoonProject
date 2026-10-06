using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.Editor.Tests
{
    /// <summary>
    /// Guards the committed ProjectSettings the game relies on (docs/ARCHITECTURE.md). If one of these fails, either
    /// the settings drifted or the architecture changed: fix the setting or update this test with the Director.
    /// </summary>
    public sealed class ProjectSettingsTests
    {
        private const string PcPipelineName = "PC_RPAsset";

        /// <summary>Layer pairs that collide, everything else among the game layers is ignored.</summary>
        private static readonly (int, int)[] CollidingPairs =
        {
            (Layers.Ground, Layers.Rover), (Layers.Ground, Layers.Relic), (Layers.Ground, Layers.Pickup),
            (Layers.Ground, Layers.Prop),
            (Layers.Rover, Layers.Relic), (Layers.Rover, Layers.Pickup), (Layers.Rover, Layers.Prop),
            (Layers.Rover, Layers.Trigger),
            (Layers.Relic, Layers.Relic), (Layers.Relic, Layers.Prop), (Layers.Relic, Layers.Trigger),
            (Layers.Prop, Layers.Prop),
        };

        private static readonly int[] GameLayers =
        {
            Layers.Ground, Layers.Rover, Layers.Relic, Layers.Pickup, Layers.Prop, Layers.Trigger,
        };

        [Test]
        public void LayerNames_MatchCoreLayers()
        {
            Assert.AreEqual("Ground", LayerMask.LayerToName(Layers.Ground));
            Assert.AreEqual("Rover", LayerMask.LayerToName(Layers.Rover));
            Assert.AreEqual("Relic", LayerMask.LayerToName(Layers.Relic));
            Assert.AreEqual("Pickup", LayerMask.LayerToName(Layers.Pickup));
            Assert.AreEqual("Prop", LayerMask.LayerToName(Layers.Prop));
            Assert.AreEqual("Trigger", LayerMask.LayerToName(Layers.Trigger));
        }

        [Test]
        public void CollisionMatrix_OnlyNeededGameLayerPairsCollide()
        {
            foreach (int a in GameLayers)
            {
                foreach (int b in GameLayers)
                {
                    bool expected = Contains(a, b);
                    Assert.AreEqual(expected, !Physics.GetIgnoreLayerCollision(a, b),
                        $"{LayerMask.LayerToName(a)} x {LayerMask.LayerToName(b)} should {(expected ? "" : "not ")}collide");
                }
            }
        }

        [Test]
        public void CollisionMatrix_DefaultLayerCollidesWithEverything()
        {
            for (int layer = 0; layer < 32; layer++)
            {
                Assert.IsFalse(Physics.GetIgnoreLayerCollision(0, layer), $"Default x layer {layer}");
            }
        }

        [Test]
        public void Physics_IsLunarAndTetherFriendly()
        {
            Assert.AreEqual(new Vector3(0f, -1.62f, 0f), Physics.gravity);
            Assert.AreEqual(0.02f, Time.fixedDeltaTime, 1e-6f);
            Assert.AreEqual(10, Physics.defaultSolverIterations);
            Assert.AreEqual(4, Physics.defaultSolverVelocityIterations);
            Assert.IsFalse(Physics.queriesHitTriggers, "queries opt in to triggers explicitly");
        }

        [Test]
        public void Player_IdentityColourSpaceAndInput()
        {
            Assert.AreEqual("MoonProject", PlayerSettings.companyName);
            Assert.AreEqual("Lofi Lunar", PlayerSettings.productName);
            Assert.AreEqual(ColorSpace.Linear, PlayerSettings.colorSpace);
            var player = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            Assert.AreEqual(1, player.FindProperty("activeInputHandler").intValue, "Input System package only");
#if ENABLE_LEGACY_INPUT_MANAGER
            Assert.Fail("The legacy Input Manager is enabled.");
#endif
        }

        [Test]
        public void Rendering_PcPipelineIsDefaultWithVSync()
        {
            Assert.AreEqual(PcPipelineName, GraphicsSettings.defaultRenderPipeline.name);
            int pc = System.Array.IndexOf(QualitySettings.names, "PC");
            Assert.GreaterOrEqual(pc, 0, "PC quality level exists");
            Assert.AreEqual(PcPipelineName, QualitySettings.GetRenderPipelineAssetAt(pc).name);
            Assert.AreEqual(pc, QualitySettings.GetQualityLevel(), "PC is the active (Standalone default) level");
            Assert.AreEqual(1, QualitySettings.vSyncCount);
        }

        private static bool Contains(int a, int b)
        {
            foreach ((int x, int y) in CollidingPairs)
            {
                if ((x == a && y == b) || (x == b && y == a))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
