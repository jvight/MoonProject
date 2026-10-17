using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using MoonProject.Art;
using MoonProject.Core;
using MoonProject.Editor.Builders;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Wraps the art box's RoverModel into the playable Rover.prefab:
    /// <code>
    /// Rover            RoverController, RoverBodyLanguage (both IGameSystems)
    ///   PhysicsSphere  Rigidbody + SphereCollider (layer Rover, frictionless)
    ///   Visual         RoverVisualRig, RoverHeadlamp, RoverFlankLamps (the battery's light bar on Body)
    ///     Chassis      jelly lean / bob
    ///       RoverModel (nested prefab; adds Headlamp spot under HeadlampSocket, and EyeGlow point and the
    ///                  LensGlass ring (nested prefab, the opening's Earth reflection) under Eye)
    ///         HeadlampSocket
    ///           Headlamp
    ///             LampMotes  RoverLampMotes: world-space dust motes in a cone along the beam
    ///   WheelFx        RoverWheelFx: TrackLeft/Right ribbons, DustLeft/Right, LandingDust
    /// </code>
    /// Fails with a clear error when RoverModel.prefab does not exist yet: there is no placeholder model. Saved only
    /// when it changed (<see cref="PrefabWriter"/>), so a re-run leaves the file byte-identical.
    /// </summary>
    public static class RoverPrefabBuilder
    {
        /// <summary>Unity's Default layer: the headlamp's dust motes stay there, out of the portrait.</summary>
        private const int DefaultLayer = 0;

        private const int DustMaxParticles = 160;
        private const int LandingMaxParticles = 240;
        private const float DustConeAngle = 14f;
        private const float DustConeRadius = 0.08f;
        private const float LandingRingRadius = 0.55f;
        private const float LandingConeAngle = 72f;

        /// <summary>How far the lighter half of the grains is lifted from the dust swatch toward white.</summary>
        private const float DustHighlight = 0.2f;

        /// <summary>Radius (m) of the lamp's lens, where the motes' cone starts.</summary>
        private const float MoteLensRadius = 0.05f;

        /// <summary>Motes are lamp-warm dust: the lamp swatch, a share of the way toward cream.</summary>
        private const float MoteCreamShare = 0.35f;

        /// <summary>How fast (per second) the curling air the motes wander on changes its pattern.</summary>
        private const float MoteNoiseScroll = 0.06f;

        /// <summary>Kicks rolling dust up and back from the rear wheel contact at a 32-deg elevation arc.</summary>
        private static readonly Vector3 DustConeRotation = new Vector3(-148f, 0f, 0f);

        [MoonBuilder("Rover/Rover", 310)]
        public static void Build()
        {
            using (var scratch = new BuilderScratchScene())
            {
                PrefabWriter.SaveIfChanged(Assemble(scratch), RoverAssetPaths.RoverPrefab);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>The Rover hierarchy described above, built in <paramref name="scratch"/> and not saved.</summary>
        public static GameObject Assemble(BuilderScratchScene scratch)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RoverAssetPaths.RoverModel);
            if (model == null
                || model.transform.Find(RoverModelNodes.SolarWing + "/" + RoverModelNodes.CellFilled) == null)
            {
                MoonProject.Art.Editor.R1ModelBuilder.Build();
            }

            model = BuildWiring.Require<GameObject>(RoverAssetPaths.RoverModel, "the Art box's rover builder");
            var lensGlass = BuildWiring.Require<GameObject>(RoverAssetPaths.LensGlass, "the Art box's glass builder");
            var focusLens = BuildWiring.Require<GameObject>(RoverAssetPaths.FocusLens, "the Art box's rover builder");
            var scanFan = BuildWiring.Require<GameObject>(RoverAssetPaths.ScanFan, "the Art box's rover builder");

            var tuning = BuildWiring.Require<RoverTuning>(RoverAssetPaths.RoverTuning, "Rover/Tuning");
            var rigTuning = BuildWiring.Require<RoverRigTuning>(RoverAssetPaths.RigTuning, "Rover/Tuning");
            var fxTuning = BuildWiring.Require<RoverFxTuning>(RoverAssetPaths.FxTuning, "Rover/Tuning");
            var characterTuning =
                BuildWiring.Require<RoverCharacterTuning>(RoverAssetPaths.CharacterTuning, "Rover/Tuning");
            var faceTuning = BuildWiring.Require<FaceTuning>(RoverAssetPaths.FaceTuning, "Rover/Tuning");
            var faceDirectorTuning =
                BuildWiring.Require<FaceDirectorTuning>(RoverAssetPaths.FaceDirectorTuning, "Rover/Tuning");
            var sphereMaterial =
                BuildWiring.Require<PhysicsMaterial>(RoverAssetPaths.SpherePhysicsMaterial, "Rover/Materials");
            var trackMaterial = BuildWiring.Require<Material>(RoverAssetPaths.TrackMaterial, "Rover/Materials");
            var dustMaterial = BuildWiring.Require<Material>(RoverAssetPaths.DustMaterial, "Rover/Materials");
            var moteMaterial = BuildWiring.Require<Material>(RoverAssetPaths.MoteMaterial, "Rover/Materials");

            GameObject root = scratch.Create("Rover");
            var controller = root.AddComponent<RoverController>();
            var bodyLanguage = root.AddComponent<RoverBodyLanguage>();

            GameObject sphere = Child(scratch, "PhysicsSphere", root.transform);
            sphere.layer = Layers.Rover;
            sphere.transform.localPosition = Vector3.up * tuning.Ground.SphereRadius;
            var body = sphere.AddComponent<Rigidbody>();
            body.mass = tuning.Ground.Mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            var collider = sphere.AddComponent<SphereCollider>();
            collider.radius = tuning.Ground.SphereRadius;
            collider.sharedMaterial = sphereMaterial;

            GameObject visual = Child(scratch, "Visual", root.transform);
            var rig = visual.AddComponent<RoverVisualRig>();
            GameObject chassis = Child(scratch, "Chassis", visual.transform);
            GameObject instance = scratch.Instantiate(model, chassis.transform);
            Transform m = instance.transform;

            Light headlamp = AddHeadlamp(scratch, BuildWiring.Node(m, RoverModelNodes.HeadlampSocket), rigTuning);
            Light eyeLight = AddEyeLight(scratch, BuildWiring.Node(m, RoverModelNodes.Eye), characterTuning);
            Mount(scratch, lensGlass, BuildWiring.Node(m, RoverModelNodes.Eye)).gameObject.SetActive(true);

            var wheels = new Object[RoverModelNodes.WheelCount];
            var arms = new Object[RoverModelNodes.WheelCount];
            var shocks = new Object[RoverModelNodes.WheelCount];
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i] = BuildWiring.Node(m, RoverModelNodes.Wheel(i));
                arms[i] = BuildWiring.Node(m, RoverModelNodes.Arm(i));
                shocks[i] = BuildWiring.Node(m, RoverModelNodes.Shock(i));
            }

            BuildWiring.Assign(rig,
                ("_tuning", rigTuning),
                ("_chassis", chassis.transform),
                ("_antenna", BuildWiring.Node(m, RoverModelNodes.Antenna)));
            BuildWiring.AssignArray(rig, "_wheels", wheels);
            BuildWiring.AssignArray(rig, "_arms", arms);
            BuildWiring.AssignArray(rig, "_shocks", shocks);

            HideKitEraDressing(m);
            var roadLight = visual.AddComponent<RoverHeadlamp>();
            BuildWiring.Assign(roadLight, ("_tuning", rigTuning), ("_headlamp", headlamp));

            RoverFlankLamps flankLamps = BuildFlankLamps(visual, m, rigTuning);
            RoverWheelFx wheelFx = BuildWheelFx(scratch, root.transform, m, fxTuning, trackMaterial, dustMaterial);
            RoverLampMotes lampMotes = BuildLampMotes(scratch, headlamp, fxTuning, moteMaterial);

            Transform eyeOrigin = BuildWiring.Node(m, RoverModelNodes.EyeOrigin);
            BuildWiring.Assign(controller,
                ("_tuning", tuning),
                ("_body", body),
                ("_sphere", collider),
                ("_visualRig", rig),
                ("_wheelFx", wheelFx),
                ("_lampMotes", lampMotes),
                ("_flankLamps", flankLamps),
                ("_headlamp", roadLight),
                ("_tetherOrigin", eyeOrigin),
                ("_cutterOrigin", BuildWiring.Node(m, RoverModelNodes.CutterOrigin)),
                ("_scanOrigin", eyeOrigin),
                ("_winchOrigin", BuildWiring.Node(m, RoverModelNodes.WinchOrigin)),
                ("_scoopOrigin", BuildWiring.Node(m, RoverModelNodes.ScoopOrigin)),
                ("_cargoSocket", BuildWiring.Node(m, RoverModelNodes.CargoSocket)));
            AssignChargeCells(controller, m);
            Transform lens = Mount(scratch, focusLens, BuildWiring.Node(m, RoverModelNodes.LensSocket));
            BuildWiring.Assign(controller, ("_focusLens", lens.gameObject),
                ("_focusLensGlass", BuildWiring.NodeComponent<Renderer>(lens, "Glass")));
            Transform hatch = BuildWiring.Node(m, RoverModelNodes.CargoHatch);
            BuildWiring.Assign(controller, ("_cargoHatch", hatch));
            BuildWiring.AssignArray(controller, "_hatchCrates", HatchCrates(m));

            Transform fan = Mount(scratch, scanFan, BuildWiring.Node(m, RoverModelNodes.ScanFanSocket));
            fan.gameObject.SetActive(true);
            RoverPortrait portrait = BuildPortrait(scratch, visual, m, rigTuning);
            BuildWiring.Assign(controller, ("_portrait", portrait));
            SetLayer(instance.transform, Layers.Rover);
            SetLayer(lampMotes.transform, DefaultLayer);

            Transform head = BuildWiring.Node(m, RoverModelNodes.Head);
            Transform screen = BuildWiring.Node(head, RoverModelNodes.Screen);
            Renderer screenRenderer = BuildWiring.NodeComponent<MeshRenderer>(head, RoverModelNodes.Screen);
            MeshFilter screenFilter = BuildWiring.NodeComponent<MeshFilter>(head, RoverModelNodes.Screen);
            var screenCollider = screen.gameObject.AddComponent<MeshCollider>();
            screenCollider.sharedMesh = BuildScreenColliderMesh(screenFilter.sharedMesh);
            Transform rearVentStrip = BuildWiring.Node(head, RoverModelNodes.RearVentStrip);
            Renderer rearVentRenderer = rearVentStrip.GetComponent<Renderer>();
            if (rearVentRenderer == null)
            {
                rearVentRenderer = head.GetComponent<MeshRenderer>();
            }

            if (rearVentRenderer == null)
            {
                throw new System.InvalidOperationException(
                    "RoverModel Head/RearVentStrip has no renderer for the rear vent mood echo.");
            }

            var faceDisplay = screen.gameObject.AddComponent<FaceDisplay>();
            BuildWiring.Assign(faceDisplay,
                ("_tuning", faceTuning),
                ("_screenRenderer", screenRenderer));

            var faceDirector = screen.gameObject.AddComponent<RoverFaceDirector>();
            BuildWiring.Assign(faceDirector,
                ("_tuning", faceDirectorTuning),
                ("_display", faceDisplay),
                ("_head", head),
                ("_rearVentStrip", rearVentStrip),
                ("_rearVentRenderer", rearVentRenderer));

            BuildWiring.Assign(bodyLanguage,
                ("_tuning", characterTuning),
                ("_rover", controller),
                ("_rig", rig),
                ("_neck", BuildWiring.Node(m, RoverModelNodes.Neck)),
                ("_neckLift", BuildWiring.Node(m, RoverModelNodes.NeckLift)),
                ("_head", head),
                ("_solarWing", BuildWiring.Node(m, RoverModelNodes.SolarWing)),
                ("_cutterTurret", BuildWiring.Node(m, RoverModelNodes.CutterTurret)),
                ("_cutterHead", BuildWiring.Node(m, RoverModelNodes.CutterHead)),
                ("_scoopArms", BuildWiring.Node(m, RoverModelNodes.ScoopArms)),
                ("_scoopBucket", BuildWiring.Node(m, RoverModelNodes.ScoopBucket)),
                ("_winchSpool", BuildWiring.Node(m, RoverModelNodes.WinchSpool)),
                ("_eyeRenderer", BuildWiring.NodeComponent<MeshRenderer>(m, RoverModelNodes.Eye)),
                ("_antennaTipRenderer", BuildWiring.NodeComponent<MeshRenderer>(m, RoverModelNodes.AntennaTip)),
                ("_eyeLight", eyeLight),
                ("_scanFan", fan.GetComponentInChildren<Renderer>(true)),
                ("_faceDirector", faceDirector));

            BuildWiring.Assign(controller,
                ("_bodyLanguage", bodyLanguage),
                ("_screenCollider", screenCollider));

            return root;
        }

        /// <summary>
        /// Creates a readable copy of the Screen mesh at <see cref="RoverAssetPaths.ScreenColliderMesh"/> so
        /// <see cref="RaycastHit.textureCoord"/> returns interpolated UVs in standalone player builds (where the
        /// imported FBX mesh is non-readable).
        /// </summary>
        private static Mesh BuildScreenColliderMesh(Mesh source)
        {
            if (source == null)
            {
                throw new System.InvalidOperationException(
                    "RoverModel Head/Screen has no sharedMesh for the diegetic screen collider.");
            }

            var copy = new Mesh
            {
                vertices = source.vertices,
                normals = source.normals,
                tangents = source.tangents,
                uv = source.uv,
                triangles = source.triangles,
                bounds = source.bounds,
            };
            copy.UploadMeshData(false);
            Mesh saved = GeneratedAssets.CreateOrReplace(copy, RoverAssetPaths.ScreenColliderMesh);
            if (!saved.isReadable || saved.vertexCount == 0 || saved.uv.Length != saved.vertexCount)
            {
                throw new System.InvalidOperationException(
                    $"{RoverAssetPaths.ScreenColliderMesh} must be readable with matching vertex and UV counts.");
            }

            return saved;
        }

        /// <summary>
        /// The Tab "07" page's portrait: a disabled camera under Visual and the sockets art has marked so far
        /// (<see cref="RoverModelNodes.EquipmentSocket"/>; empty where there is none, so the page shows no marker).
        /// </summary>
        private static RoverPortrait BuildPortrait(BuilderScratchScene scratch, GameObject visual, Transform model,
            RoverRigTuning tuning)
        {
            var camera = Child(scratch, "PortraitCamera", visual.transform).AddComponent<Camera>();
            camera.enabled = false;
            var key = Child(scratch, "PortraitKey", visual.transform).AddComponent<Light>();
            var rim = Child(scratch, "PortraitRim", visual.transform).AddComponent<Light>();
            key.enabled = false;
            rim.enabled = false;
            var portrait = visual.AddComponent<RoverPortrait>();
            Transform[] nodes = model.GetComponentsInChildren<Transform>(true);
            var sockets = new Object[System.Enum.GetValues(typeof(RoverSocket)).Length];
            for (int i = 0; i < sockets.Length; i++)
            {
                sockets[i] = Find(nodes, RoverModelNodes.EquipmentSocket((RoverSocket)i));
            }

            BuildWiring.Assign(portrait, ("_tuning", tuning), ("_camera", camera), ("_key", key), ("_rim", rim));
            BuildWiring.AssignArray(portrait, "_sockets", sockets);
            return portrait;
        }

        /// <summary>07's model and its mounted kit on the Rover layer, the only one the portrait camera sees (none
        /// of it has a collider); the headlamp's dust motes go back to Default so they stay out of the portrait.
        /// </summary>
        private static void SetLayer(Transform root, int layer)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                node.gameObject.layer = layer;
            }
        }

        private static GameObject Child(BuilderScratchScene scratch, string name, Transform parent)
        {
            return scratch.Create(name, parent);
        }

        private static Light AddHeadlamp(BuilderScratchScene scratch, Transform socket, RoverRigTuning tuning)
        {
            var light = Child(scratch, "Headlamp", socket).AddComponent<Light>();
            light.transform.localRotation = Quaternion.Euler(tuning.HeadlampDownTilt, 0f, 0f);
            light.type = LightType.Spot;
            light.color = Palette.Get(PaletteSwatch.WarmLamp);
            light.intensity = tuning.HeadlampIntensity;
            light.range = tuning.HeadlampRange;
            light.spotAngle = tuning.HeadlampSpotAngle;
            light.innerSpotAngle = tuning.HeadlampInnerSpotAngle;
            // Its shadows fall behind what it lights, away from the chase camera: an extra shadow map for nothing seen.
            light.shadows = LightShadows.None;
            return light;
        }

        private static Light AddEyeLight(BuilderScratchScene scratch, Transform eye, RoverCharacterTuning tuning)
        {
            var light = Child(scratch, "EyeGlow", eye).AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Palette.Get(PaletteSwatch.WarmLamp);
            light.intensity = tuning.EyeLightIntensity;
            light.range = tuning.EyeLightRange;
            light.shadows = LightShadows.None;
            return light;
        }

        /// <summary>
        /// The model's solar-cell patch, fresh serial and pennant belong to the retired kit-and-gifts progression: they
        /// stay hidden so 07 shows the same worn body from the first frame.
        /// </summary>
        private static void HideKitEraDressing(Transform model)
        {
            BuildWiring.Node(model, RoverModelNodes.CellFilled).gameObject.SetActive(false);
            BuildWiring.Node(model, RoverModelNodes.Decal07Fresh).gameObject.SetActive(false);
            BuildWiring.Node(model, RoverModelNodes.Pennant).gameObject.SetActive(false);
        }

        /// <summary>The battery's light-bar segments already in RoverModel, shown per capacity.</summary>
        private static RoverFlankLamps BuildFlankLamps(GameObject host, Transform model, RoverRigTuning tuning)
        {
            var lamps = new Object[RoverModelNodes.FlankLampCount];
            var glasses = new Object[RoverModelNodes.FlankLampCount];
            for (int i = 0; i < lamps.Length; i++)
            {
                lamps[i] = BuildWiring.Node(model, RoverModelNodes.FlankLamp(i));
                glasses[i] = BuildWiring.NodeComponent<MeshRenderer>(model, RoverModelNodes.FlankGlass(i));
            }

            var flankLamps = host.AddComponent<RoverFlankLamps>();
            BuildWiring.Assign(flankLamps, ("_tuning", tuning));
            BuildWiring.AssignArray(flankLamps, "_lamps", lamps);
            BuildWiring.AssignArray(flankLamps, "_glasses", glasses);
            return flankLamps;
        }

        private static RoverWheelFx BuildWheelFx(BuilderScratchScene scratch, Transform root, Transform model,
            RoverFxTuning tuning, Material trackMaterial, Material dustMaterial)
        {
            GameObject host = Child(scratch, "WheelFx", root);
            var wheelFx = host.AddComponent<RoverWheelFx>();
            BuildWiring.Assign(wheelFx,
                ("_tuning", tuning),
                ("_frontSocketLeft", BuildWiring.Node(model, RoverModelNodes.Wheel(0))),
                ("_frontSocketRight", BuildWiring.Node(model, RoverModelNodes.Wheel(1))),
                ("_dustSocketLeft", BuildWiring.Node(model, RoverModelNodes.DustSocketLeft)),
                ("_dustSocketRight", BuildWiring.Node(model, RoverModelNodes.DustSocketRight)),
                ("_trackFrontLeft", Track(scratch, "TrackFrontLeft", host.transform, trackMaterial)),
                ("_trackFrontRight", Track(scratch, "TrackFrontRight", host.transform, trackMaterial)),
                ("_trackLeft", Track(scratch, "TrackLeft", host.transform, trackMaterial)),
                ("_trackRight", Track(scratch, "TrackRight", host.transform, trackMaterial)),
                ("_dustLeft", Dust(scratch, "DustLeft", host.transform, dustMaterial)),
                ("_dustRight", Dust(scratch, "DustRight", host.transform, dustMaterial)),
                ("_landingDust", LandingRing(scratch, host.transform, dustMaterial)));
            return wheelFx;
        }

        /// <summary>
        /// The charge-up's Cells (0.5.5 §5.4): every ConduitGlow renderer (the deck pair and the neck's), the
        /// StageGlow_n_L/R sleeve of each Stage_n_L and Stage_n_R (stage 1 first), and the eye's LockGlow.
        /// </summary>
        private static void AssignChargeCells(RoverController controller, Transform model)
        {
            var conduits = new List<Object>();
            foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == RoverModelNodes.ConduitGlow)
                {
                    conduits.Add(BuildWiring.NodeComponent<Renderer>(node.parent, RoverModelNodes.ConduitGlow));
                }
            }

            if (conduits.Count == 0)
            {
                throw new System.InvalidOperationException(
                    "RoverModel has no ConduitGlow (the charge-up's Cells contract, 0.5.5).");
            }

            var sleeves = new Object[2 * RoverModelNodes.ChargeStages];
            for (int stage = 1; stage <= RoverModelNodes.ChargeStages; stage++)
            {
                for (int side = 0; side < 2; side++)
                {
                    bool left = side == 0;
                    Transform cell = BuildWiring.Node(model, RoverModelNodes.StageCell(stage, left));
                    sleeves[2 * (stage - 1) + side] =
                        BuildWiring.NodeComponent<Renderer>(cell, RoverModelNodes.StageGlow(stage, left));
                }
            }

            BuildWiring.AssignArray(controller, "_conduitGlows", conduits.ToArray());
            BuildWiring.AssignArray(controller, "_stageSleeves", sleeves);
            BuildWiring.Assign(controller,
                ("_lockGlow", BuildWiring.NodeComponent<Renderer>(model, RoverModelNodes.LockGlow)));
        }

        /// <summary>The hatch window's HatchCrate_0, HatchCrate_1, ... nodes in order; at least one, no gaps.</summary>
        private static Object[] HatchCrates(Transform model)
        {
            Transform[] nodes = model.GetComponentsInChildren<Transform>(true);
            var crates = new List<Object>();
            for (int i = 0; ; i++)
            {
                Transform crate = Find(nodes, RoverModelNodes.HatchCrate(i));
                if (crate == null)
                {
                    break;
                }

                crate.gameObject.SetActive(false);
                crates.Add(crate.gameObject);
            }

            if (crates.Count == 0)
            {
                throw new System.InvalidOperationException(
                    "CargoHatch has no HatchCrate_0 (the hatch window's crate contract, M3-25).");
            }

            return crates.ToArray();
        }

        private static Transform Find(Transform[] nodes, string name)
        {
            foreach (Transform node in nodes)
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>A kit prefab on <paramref name="socket"/> with identity, hidden until 07 owns it.</summary>
        private static Transform Mount(BuilderScratchScene scratch, GameObject prefab, Transform socket)
        {
            Transform mount = scratch.Instantiate(prefab, socket).transform;
            mount.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            mount.localScale = Vector3.one;
            mount.gameObject.SetActive(false);
            return mount;
        }

        /// <summary>
        /// Dust motes in the beam: a world-space cone volume along the lamp's +Z with random directions and slow noise,
        /// started invisible (RoverLampMotes sets each mote's opacity from the light it is in, and the tuned amounts).
        /// </summary>
        private static RoverLampMotes BuildLampMotes(BuilderScratchScene scratch, Light headlamp, RoverFxTuning tuning,
            Material material)
        {
            GameObject host = Child(scratch, "LampMotes", headlamp.transform);
            var system = host.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 1f;
            main.maxParticles = tuning.MoteCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = tuning.MoteLifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(tuning.MoteMinSize, tuning.MoteMaxSize);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, tuning.MoteDrift);
            Color tint = Color.Lerp(Palette.Get(PaletteSwatch.WarmLamp), Palette.Get(PaletteSwatch.Cream),
                MoteCreamShare);
            tint.a = 0f;
            main.startColor = tint;
            main.gravityModifier = 1f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.ConeVolume;
            shape.angle = tuning.MoteSpread;
            shape.radius = MoteLensRadius;
            shape.radiusThickness = 1f;
            shape.length = tuning.MoteReach;
            shape.randomDirectionAmount = 0.35f;

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = false;
            noise.strength = 0f;
            noise.frequency = tuning.MoteWanderFrequency;
            noise.scrollSpeed = MoteNoiseScroll;
            noise.octaveCount = 1;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low;

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.None;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var motes = host.AddComponent<RoverLampMotes>();
            BuildWiring.Assign(motes, ("_tuning", tuning), ("_motes", system), ("_headlamp", headlamp));
            return motes;
        }

        private static RoverTrackRenderer Track(BuilderScratchScene scratch, string name, Transform parent,
            Material material)
        {
            GameObject track = Child(scratch, name, parent);
            track.AddComponent<MeshFilter>();
            var renderer = track.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return track.AddComponent<RoverTrackRenderer>();
        }

        /// <summary>Rolling dust: ballistic grains kicked up and back in a clean lunar parabola (rate at runtime).</summary>
        private static ParticleSystem Dust(BuilderScratchScene scratch, string name, Transform parent,
            Material material)
        {
            ParticleSystem dust = CreateSystem(scratch, name, parent, material, DustMaxParticles, true);

            ParticleSystem.ShapeModule shape = dust.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = DustConeAngle;
            shape.radius = DustConeRadius;
            shape.rotation = DustConeRotation;

            ParticleSystem.LimitVelocityOverLifetimeModule drag = dust.limitVelocityOverLifetime;
            drag.enabled = false;
            drag.drag = 0f;
            return dust;
        }

        /// <summary>Landing splash ring: grains sprayed outward along a shallow 18-deg cone and falling ballistically.</summary>
        private static ParticleSystem LandingRing(BuilderScratchScene scratch, Transform parent, Material material)
        {
            ParticleSystem ring = CreateSystem(scratch, "LandingDust", parent, material, LandingMaxParticles, false);

            ParticleSystem.ShapeModule shape = ring.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = LandingConeAngle;
            shape.radius = LandingRingRadius;
            shape.radiusThickness = 0f;
            shape.arc = 360f;

            ParticleSystem.VelocityOverLifetimeModule lift = ring.velocityOverLifetime;
            lift.enabled = false;

            ParticleSystem.LimitVelocityOverLifetimeModule drag = ring.limitVelocityOverLifetime;
            drag.enabled = false;
            drag.drag = 0f;
            return ring;
        }

        /// <summary>
        /// Shared particle setup. Start values are normalised ranges around 1; <see cref="RoverWheelFx"/> scales them
        /// with the tuned lifetime, size, speed and gravity multipliers.
        /// </summary>
        private static ParticleSystem CreateSystem(BuilderScratchScene scratch, string name, Transform parent,
            Material material, int maxParticles, bool looping)
        {
            GameObject host = Child(scratch, name, parent);
            var system = host.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = looping;
            main.playOnAwake = looping;
            main.duration = 1f;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.65f, 1.35f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            Color dust = Palette.Get(PaletteSwatch.DustLight);
            main.startColor = new ParticleSystem.MinMaxGradient(dust, Color.Lerp(dust, Color.white, DustHighlight));
            main.gravityModifier = 1f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 0f;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.95f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0.65f)));

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.55f, 0.45f),
                    new GradientAlphaKey(0f, 1f),
                });
            color.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystem.RotationOverLifetimeModule tumble = system.rotationOverLifetime;
            tumble.enabled = true;
            tumble.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }
    }
}
