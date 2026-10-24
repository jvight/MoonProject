using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Art;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// EditMode tests for The Long Mare (<c>0.6</c>) R1 environment kit:
    ///   1. Mare ground material (<c>R1_MareBasalt</c> with tiling-break detail) + pale swirl decal (<c>R1_MareSwirl</c> /
    ///      <c>Decal_MareSwirl</c>) + pale ejecta-ray decal (<c>R1_MareEjectaRay</c> / <c>Decal_MareEjectaRay</c>).
    ///   2. Mare rock kit: 6 basalt boulders/outcrops (<c>MareRock_00..05</c>) + 2 dark Ilmenite outcrops
    ///      (<c>MareOutcrop_Ilmenite_00..01</c> with subtle non-emissive metallic glint and <c>DrillTarget_Anchor</c>).
    ///   3. Rille pass modular pieces: 4 wall/slope chunks (<c>Rille_Wall_Straight</c>, <c>Rille_Wall_Curve</c>,
    ///      <c>Rille_Slope_Ramp</c>, <c>Rille_Mouth_Corner</c>) with <c>Start_Anchor</c> and <c>End_Anchor</c>.
    ///   4. Mare derelict set: stopped convoy (<c>Derelict_MareConvoy</c>: 3 haul bots in a line, middle with
    ///      <c>StrappedLoad</c>), crashed survey drone (<c>Derelict_MareSurveyDrone</c>: vacuum De Laval RCS nozzles),
    ///      and half-buried tardigrade experiment capsule (<c>Derelict_TardigradeCapsule</c>: nod to Beresheet,
    ///      bloom-safe <c>TinyWindow</c>).
    /// </summary>
    public sealed class R1MareKitTests
    {
        [OneTimeSetUp]
        public void EnsureMareKitBuilt()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(R1MareKitBuilder.MareBasaltMatPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(R1MareKitBuilder.DerelictTardigradeCapsulePrefabPath) == null)
            {
                R1MareKitBuilder.Build();
            }
        }

        [Test]
        public void MareTextures_AllTwelveExist_WithCorrectImportSettings()
        {
            string[] textureSets =
            {
                "T_R1_MareBasalt",
                "T_R1_MareSwirl",
                "T_R1_MareEjectaRay",
                "T_R1_Ilmenite",
            };

            for (int i = 0; i < textureSets.Length; i++)
            {
                string prefix = R1MaterialRecipes.TexturesFolder + "/" + textureSets[i];
                string albedoPath = prefix + "_Albedo.png";
                string normalPath = prefix + "_Normal.png";
                string maskPath = prefix + "_Mask.png";

                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);

                Assert.IsNotNull(albedo, $"Missing Mare albedo texture at '{albedoPath}'.");
                Assert.IsNotNull(normal, $"Missing Mare normal texture at '{normalPath}'.");
                Assert.IsNotNull(mask, $"Missing Mare mask texture at '{maskPath}'.");

                var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                var maskImporter = AssetImporter.GetAtPath(maskPath) as TextureImporter;
                Assert.IsNotNull(normalImporter);
                Assert.IsNotNull(maskImporter);
                Assert.AreEqual(TextureImporterType.NormalMap, normalImporter.textureType,
                    $"'{normalPath}' must be imported as NormalMap.");
                Assert.IsFalse(maskImporter.sRGBTexture,
                    $"'{maskPath}' must be imported as linear (sRGBTexture = false).");
            }
        }

        [Test]
        public void MareMaterials_Exist_HaveTilingBreak_AndObeyEmissionRules()
        {
            var mareBasalt = AssetDatabase.LoadAssetAtPath<Material>(R1MareKitBuilder.MareBasaltMatPath);
            var mareSwirl = AssetDatabase.LoadAssetAtPath<Material>(R1MareKitBuilder.MareSwirlMatPath);
            var mareEjectaRay = AssetDatabase.LoadAssetAtPath<Material>(R1MareKitBuilder.MareEjectaRayMatPath);
            var ilmenite = AssetDatabase.LoadAssetAtPath<Material>(R1MareKitBuilder.IlmeniteMatPath);
            var tardigradeWin = AssetDatabase.LoadAssetAtPath<Material>(R1MareKitBuilder.TardigradeWindowMatPath);

            Assert.IsNotNull(mareBasalt, "Missing R1_MareBasalt.mat");
            Assert.IsNotNull(mareSwirl, "Missing R1_MareSwirl.mat");
            Assert.IsNotNull(mareEjectaRay, "Missing R1_MareEjectaRay.mat");
            Assert.IsNotNull(ilmenite, "Missing R1_Ilmenite.mat");
            Assert.IsNotNull(tardigradeWin, "Missing R1_TardigradeWindow.mat");

            // 1. MareBasalt: tiling-break detail & zero emission
            Assert.AreEqual(R1MaterialRecipes.ShaderName, mareBasalt.shader.name);
            Assert.GreaterOrEqual(mareBasalt.GetFloat("_ColorVarAmount"), 0.05f,
                "R1_MareBasalt must have world-space tiling-break detail (_ColorVarAmount >= 0.05).");
            Assert.AreEqual(0f, mareBasalt.GetFloat("_EmissionStrength"), 1e-5f);

            // 2. MareSwirl & MareEjectaRay: zero emission (high-albedo dust only, never glowing)
            Assert.AreEqual(0f, mareSwirl.GetFloat("_EmissionStrength"), 1e-5f);
            Assert.AreEqual(0f, mareEjectaRay.GetFloat("_EmissionStrength"), 1e-5f);

            // 3. Ilmenite: strictly non-emissive FeTiO3 with warm bronze-black/gunmetal metallic glint
            Assert.AreEqual(0f, ilmenite.GetFloat("_EmissionStrength"), 1e-5f,
                "R1_Ilmenite must have zero emission (FeTiO3 is non-emissive).");
            Assert.AreEqual(Color.black, ilmenite.GetColor("_EmissionColor"));
            Assert.GreaterOrEqual(ilmenite.GetFloat("_EdgeWearMetallic"), 0.80f,
                "R1_Ilmenite must have a metallic crystal cleavage glint.");
            Assert.GreaterOrEqual(ilmenite.GetFloat("_SmoothnessMax"), 0.45f);

            // 4. TardigradeWindow: warm amber dormant glow, strictly bloom-safe
            Assert.Greater(tardigradeWin.GetFloat("_EmissionStrength"), 0.5f);
            Assert.Less(tardigradeWin.GetFloat("_BloomSafeMax"), Palette.BloomThreshold,
                "R1_TardigradeWindow body emission must remain below Palette.BloomThreshold.");
        }

        [Test]
        public void MareGroundAndDecals_Exist_WithShadowOff_AndNoDecalColliders()
        {
            var groundPatch = AssetDatabase.LoadAssetAtPath<GameObject>(R1MareKitBuilder.MareGroundPatchPrefabPath);
            var swirlDecal = AssetDatabase.LoadAssetAtPath<GameObject>(R1MareKitBuilder.DecalMareSwirlPrefabPath);
            var rayDecal = AssetDatabase.LoadAssetAtPath<GameObject>(R1MareKitBuilder.DecalMareEjectaRayPrefabPath);

            Assert.IsNotNull(groundPatch, "Missing MareGround_Patch.prefab");
            Assert.IsNotNull(swirlDecal, "Missing Decal_MareSwirl.prefab");
            Assert.IsNotNull(rayDecal, "Missing Decal_MareEjectaRay.prefab");

            var groundRenderer = groundPatch.GetComponentInChildren<Renderer>();
            Assert.IsNotNull(groundRenderer);
            Assert.AreEqual("R1_MareBasalt", groundRenderer.sharedMaterial.name);

            foreach ((GameObject decal, string expectedMat) in new[]
            {
                (swirlDecal, "R1_MareSwirl"),
                (rayDecal, "R1_MareEjectaRay"),
            })
            {
                Assert.IsEmpty(decal.GetComponentsInChildren<Collider>(true),
                    $"Ground decal '{decal.name}' must have no Colliders so Rover 07's wheels never bump on decals.");
                Renderer[] renderers = decal.GetComponentsInChildren<Renderer>(true);
                Assert.IsNotEmpty(renderers);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Assert.AreEqual(ShadowCastingMode.Off, renderers[i].shadowCastingMode);
                    Assert.AreEqual(expectedMat, renderers[i].sharedMaterial.name);
                }
            }
        }

        [Test]
        public void MareRockKit_HasSixBasaltRocks_AndTwoIlmeniteOutcropsWithDrillTarget()
        {
            Assert.AreEqual(6, R1MareKitBuilder.MareBasaltRockPrefabPaths.Length);
            for (int i = 0; i < R1MareKitBuilder.MareBasaltRockPrefabPaths.Length; i++)
            {
                string path = R1MareKitBuilder.MareBasaltRockPrefabPaths[i];
                var rock = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(rock, $"Missing Mare basalt rock prefab at '{path}'.");
                var renderer = rock.GetComponentInChildren<Renderer>();
                Assert.IsNotNull(renderer);
                Assert.AreEqual("R1_MareBasalt", renderer.sharedMaterial.name);
                Assert.IsNotNull(rock.GetComponentInChildren<Collider>(),
                    $"Mare basalt rock '{rock.name}' must have a Collider.");
            }

            string[] ilmenitePaths =
            {
                R1MareKitBuilder.IlmeniteOutcrop00PrefabPath,
                R1MareKitBuilder.IlmeniteOutcrop01PrefabPath,
            };
            for (int i = 0; i < ilmenitePaths.Length; i++)
            {
                var outcrop = AssetDatabase.LoadAssetAtPath<GameObject>(ilmenitePaths[i]);
                Assert.IsNotNull(outcrop, $"Missing Ilmenite outcrop prefab at '{ilmenitePaths[i]}'.");

                Transform hostRock = outcrop.transform.Find("HostRock");
                Transform vein = outcrop.transform.Find("IlmeniteVein");
                Transform drillAnchor = outcrop.transform.Find("DrillTarget_Anchor");

                Assert.IsNotNull(hostRock, $"'{outcrop.name}' missing HostRock child.");
                Assert.IsNotNull(vein, $"'{outcrop.name}' missing IlmeniteVein child.");
                Assert.IsNotNull(drillAnchor, $"'{outcrop.name}' missing DrillTarget_Anchor child.");

                Assert.AreEqual("R1_MareBasalt", hostRock.GetComponent<Renderer>().sharedMaterial.name);
                Material veinMat = vein.GetComponent<Renderer>().sharedMaterial;
                Assert.AreEqual("R1_Ilmenite", veinMat.name);
                Assert.AreEqual(0f, veinMat.GetFloat("_EmissionStrength"), 1e-5f);

                float drillHeightM = drillAnchor.position.y - outcrop.transform.position.y;
                Assert.That(drillHeightM, Is.InRange(0.35f, 0.95f),
                    $"'{outcrop.name}' DrillTarget_Anchor height ({drillHeightM:F2} m) must sit at 07's drill reach.");
            }
        }

        [Test]
        public void RillePassPieces_AllFourChunksExist_WithAnchorsAndMeshColliders()
        {
            string[] rillePrefabPaths =
            {
                R1MareKitBuilder.RilleWallStraightPrefabPath,
                R1MareKitBuilder.RilleWallCurvePrefabPath,
                R1MareKitBuilder.RilleSlopeRampPrefabPath,
                R1MareKitBuilder.RilleMouthCornerPrefabPath,
            };

            for (int i = 0; i < rillePrefabPaths.Length; i++)
            {
                var piece = AssetDatabase.LoadAssetAtPath<GameObject>(rillePrefabPaths[i]);
                Assert.IsNotNull(piece, $"Missing rille pass modular piece at '{rillePrefabPaths[i]}'.");
                Assert.IsNotNull(piece.transform.Find("Start_Anchor"),
                    $"'{piece.name}' missing Start_Anchor.");
                Assert.IsNotNull(piece.transform.Find("End_Anchor"),
                    $"'{piece.name}' missing End_Anchor.");
                Assert.IsNotEmpty(piece.GetComponentsInChildren<MeshCollider>(),
                    $"'{piece.name}' must have MeshColliders for rille channel walls/slopes.");
            }
        }

        [Test]
        public void MareDerelicts_Convoy_SurveyDrone_AndTardigradeCapsule_MeetAllSpecRules()
        {
            // 1. Stopped Convoy (3 haul bots in a line, middle with StrappedLoad, all dark, no floating loot)
            var convoy = AssetDatabase.LoadAssetAtPath<GameObject>(R1MareKitBuilder.DerelictMareConvoyPrefabPath);
            Assert.IsNotNull(convoy, "Missing Derelict_MareConvoy.prefab");
            Assert.IsNotNull(convoy.transform.Find("HaulBot_Lead"));
            Assert.IsNotNull(convoy.transform.Find("HaulBot_Cargo"));
            Assert.IsNotNull(convoy.transform.Find("HaulBot_Rear"));
            Assert.IsNotNull(convoy.transform.Find("StrappedLoad"),
                "Convoy must have one haul bot carrying a StrappedLoad.");
            Assert.IsNotNull(convoy.transform.Find("CutPoint"));
            Assert.IsNotNull(convoy.transform.Find("FragmentSocket_CutterRibs_0"));
            Assert.IsNotNull(convoy.transform.Find("Decal_KA_1"));
            Assert.IsNotNull(convoy.transform.Find("Decal_ILC_1"));

            foreach (Renderer r in convoy.GetComponentsInChildren<Renderer>(true))
            {
                if (r.sharedMaterial != null
                    && r.sharedMaterial.HasProperty("_EmissionStrength")
                    && r.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    float effectiveEmission = r.sharedMaterial.GetFloat("_EmissionStrength")
                        * r.sharedMaterial.GetColor("_EmissionColor").maxColorComponent;
                    Assert.AreEqual(0f, effectiveEmission, 1e-5f,
                        $"Stopped convoy renderer '{r.name}' must be dark (zero emission).");
                }
            }

            // 2. Crashed Survey Drone (vacuum De Laval RCS thrusters, impact furrow, dead eye, zero emission)
            var drone = AssetDatabase.LoadAssetAtPath<GameObject>(R1MareKitBuilder.DerelictMareSurveyDronePrefabPath);
            Assert.IsNotNull(drone, "Missing Derelict_MareSurveyDrone.prefab");
            Assert.IsNotNull(drone.transform.Find("ImpactFurrow"));
            Assert.IsNotNull(drone.transform.Find("DroneBody"));
            Assert.IsNotNull(drone.transform.Find("ThrusterPods"),
                "Survey drone must use vacuum RCS thruster pods (never rotors/propellers/wings).");
            Assert.IsNotNull(drone.transform.Find("SurveyBoom"));
            Assert.IsNotNull(drone.transform.Find("Eye_Dead"));
            Assert.IsNotNull(drone.transform.Find("CutPoint"));
            Assert.IsNotNull(drone.transform.Find("FragmentSocket_Spool_0"));
            Assert.IsNotNull(drone.transform.Find("Decal_Tycho_1"));

            foreach (Renderer r in drone.GetComponentsInChildren<Renderer>(true))
            {
                if (r.sharedMaterial != null
                    && r.sharedMaterial.HasProperty("_EmissionStrength")
                    && r.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    float effectiveEmission = r.sharedMaterial.GetFloat("_EmissionStrength")
                        * r.sharedMaterial.GetColor("_EmissionColor").maxColorComponent;
                    Assert.AreEqual(0f, effectiveEmission, 1e-5f,
                        $"Crashed survey drone renderer '{r.name}' must be dark (zero emission).");
                }
            }

            // 3. Half-Buried Tardigrade Experiment Capsule (small, Beresheet nod, TinyWindow warm amber glow)
            var capsule = AssetDatabase.LoadAssetAtPath<GameObject>(R1MareKitBuilder.DerelictTardigradeCapsulePrefabPath);
            Assert.IsNotNull(capsule, "Missing Derelict_TardigradeCapsule.prefab");
            Assert.IsNotNull(capsule.transform.Find("BurialMound"));
            Assert.IsNotNull(capsule.transform.Find("CapsuleBody"));
            Assert.IsNotNull(capsule.transform.Find("ArchivePlaque"));
            Transform tinyWin = capsule.transform.Find("TinyWindow");
            Assert.IsNotNull(tinyWin, "Tardigrade capsule must have TinyWindow child.");
            Assert.AreEqual("R1_TardigradeWindow", tinyWin.GetComponent<Renderer>().sharedMaterial.name);
            Assert.IsNotNull(capsule.transform.Find("ScanTarget_Anchor"));
            Assert.IsNotNull(capsule.transform.Find("CarryGrip_Anchor"));
            Assert.IsNotNull(capsule.transform.Find("Window_Anchor"));
            Assert.IsNotNull(capsule.transform.Find("Decal_ILC_1"));
        }
    }
}
