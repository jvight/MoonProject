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
    ///   Visual         RoverVisualRig, RoverHoverCoils, RoverKit, RoverFlankLamps (the battery's light bar on Body)
    ///     Chassis      jelly lean / bob
    ///       RoverModel (nested prefab; adds Headlamp spot under HeadlampSocket, and EyeGlow point and the
    ///                  LensGlass ring (nested prefab, the opening's Earth reflection) under Eye)
    ///         HeadlampSocket
    ///           Headlamp
    ///             LampMotes  RoverLampMotes: world-space dust motes in a cone along the beam
    ///           Kit_LampBar (nested prefab, inactive until 07 owns the Warm Headlamp)
    ///         DrumSocket_L, DrumSocket_R
    ///           Kit_CapacitorDrum (nested prefab twice, inactive until 07 owns the Boost Coils)
    ///         CargoSocket
    ///           Kit_CargoRack (nested prefab, inactive until 07 owns the Cargo Cradle)
    ///         SpineSocket
    ///           Kit_BatterySpine (nested prefab, inactive until 07 owns Cell Pack II)
    ///         Neck/Head/RingSocket
    ///           Kit_TetherRing (nested prefab, inactive until Kenji's garage port fits it)
    ///         DeckSocket
    ///           Kit_TapeDeck (nested prefab, inactive until 07 has picked up Tilly's tape deck)
    ///         TubeSocket
    ///           BlueprintTube (nested prefab, inactive until 07 carries a scanned blueprint)
    ///           Part_TowerCoil, Part_Transformer, Part_RelaySpare (nested prefabs, one shown while 07 carries it)
    ///         CoilSocket
    ///           HoverCoils (nested prefab, inactive until 07 owns the Hover-Jump)
    ///           CoilGlow   soft cyan point light, off until the jump charges
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
        private const float DustConeAngle = 30f;
        private const float DustConeRadius = 0.15f;
        private const float LandingRingRadius = 0.7f;

        /// <summary>Dust drifts on the tuned drag; the builder sets a unit curve for it to scale.</summary>
        private const float UnitDrag = 1f;

        /// <summary>How far the lighter half of the puffs is lifted from the dust swatch toward white.</summary>
        private const float DustHighlight = 0.2f;

        /// <summary>Radius (m) of the lamp's lens, where the motes' cone starts.</summary>
        private const float MoteLensRadius = 0.05f;

        /// <summary>Motes are lamp-warm dust: the lamp swatch, a share of the way toward cream.</summary>
        private const float MoteCreamShare = 0.35f;

        /// <summary>How fast (per second) the curling air the motes wander on changes its pattern.</summary>
        private const float MoteNoiseScroll = 0.06f;

        /// <summary>Kicks rolling dust up and back from the rear wheel contact (cone axis tipped back).</summary>
        private static readonly Vector3 DustConeRotation = new Vector3(-120f, 0f, 0f);

        [MoonBuilder("Rover/Rover", 310)]
        public static void Build()
        {
            using (var scratch = new BuilderScratchScene())
            {
                PrefabWriter.SaveIfChanged(Assemble(scratch), RoverAssetPaths.RoverPrefab);
            }
        }

        /// <summary>The Rover hierarchy described above, built in <paramref name="scratch"/> and not saved.</summary>
        public static GameObject Assemble(BuilderScratchScene scratch)
        {
            var model = BuildWiring.Require<GameObject>(RoverAssetPaths.RoverModel, "the Art box's rover builder");
            var coils = BuildWiring.Require<GameObject>(RoverAssetPaths.HoverCoils, "the Art box's rover builder");
            var lampBar = BuildWiring.Require<GameObject>(RoverAssetPaths.KitLampBar, "the Art box's kit builder");
            var lensGlass = BuildWiring.Require<GameObject>(RoverAssetPaths.LensGlass, "the Art box's glass builder");
            var drum = BuildWiring.Require<GameObject>(RoverAssetPaths.KitCapacitorDrum, "the Art box's kit builder");
            var rack = BuildWiring.Require<GameObject>(RoverAssetPaths.KitCargoRack, "the Art box's kit builder");
            var spine = BuildWiring.Require<GameObject>(RoverAssetPaths.KitBatterySpine, "the Art box's kit builder");
            var deck = BuildWiring.Require<GameObject>(RoverAssetPaths.KitTapeDeck, "the Art box's kit builder");
            var ring = BuildWiring.Require<GameObject>(RoverAssetPaths.KitTetherRing, "the Art box's kit builder");
            var tube = BuildWiring.Require<GameObject>(RoverAssetPaths.BlueprintTube, "the Art box's pickup builder");
            var focusLens = BuildWiring.Require<GameObject>(RoverAssetPaths.FocusLens, "the Art box's rover builder");
            var sideBin = BuildWiring.Require<GameObject>(RoverAssetPaths.SideBin, "the Art box's kit builder");
            var parts = new GameObject[RoverAssetPaths.StoryParts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = BuildWiring.Require<GameObject>(RoverAssetPaths.StoryParts[i], "the Art box's site builder");
            }

            var tuning = BuildWiring.Require<RoverTuning>(RoverAssetPaths.RoverTuning, "Rover/Tuning");
            var rigTuning = BuildWiring.Require<RoverRigTuning>(RoverAssetPaths.RigTuning, "Rover/Tuning");
            var fxTuning = BuildWiring.Require<RoverFxTuning>(RoverAssetPaths.FxTuning, "Rover/Tuning");
            var characterTuning =
                BuildWiring.Require<RoverCharacterTuning>(RoverAssetPaths.CharacterTuning, "Rover/Tuning");
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
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i] = BuildWiring.Node(m, RoverModelNodes.Wheel(i));
            }

            BuildWiring.Assign(rig,
                ("_tuning", rigTuning),
                ("_chassis", chassis.transform),
                ("_bogieLeft", BuildWiring.Node(m, RoverModelNodes.BogieLeft)),
                ("_bogieRight", BuildWiring.Node(m, RoverModelNodes.BogieRight)),
                ("_antenna", BuildWiring.Node(m, RoverModelNodes.Antenna)));
            BuildWiring.AssignArray(rig, "_wheels", wheels);
            Transform coilSocket = BuildWiring.Node(m, RoverModelNodes.CoilSocket);
            RoverHoverCoils hoverCoils = BuildHoverCoils(scratch, visual, coilSocket, coils, rigTuning);
            RoverKit kit = BuildKit(scratch, visual, m, headlamp, rigTuning, lampBar, drum, rack, spine, deck, ring, tube,
                parts);

            RoverFlankLamps flankLamps = BuildFlankLamps(visual, m, rigTuning);
            RoverWheelFx wheelFx = BuildWheelFx(scratch, root.transform, m, fxTuning, trackMaterial, dustMaterial);
            RoverLampMotes lampMotes = BuildLampMotes(scratch, headlamp, fxTuning, moteMaterial);

            BuildWiring.Assign(controller,
                ("_tuning", tuning),
                ("_body", body),
                ("_sphere", collider),
                ("_visualRig", rig),
                ("_wheelFx", wheelFx),
                ("_hoverCoils", hoverCoils),
                ("_lampMotes", lampMotes),
                ("_flankLamps", flankLamps),
                ("_kit", kit),
                ("_tetherOrigin", BuildWiring.Node(m, RoverModelNodes.TetherOrigin)),
                ("_cargoSocket", BuildWiring.Node(m, RoverModelNodes.CargoSocket)));
            AssignChargeCells(controller, m);
            Transform lens = Mount(scratch, focusLens, BuildWiring.Node(m, RoverModelNodes.LensSocket));
            BuildWiring.Assign(controller, ("_focusLens", lens.gameObject),
                ("_focusLensGlass", BuildWiring.NodeComponent<Renderer>(lens, "Glass")));
            Transform hatch = BuildWiring.Node(m, RoverModelNodes.CargoHatch);
            BuildWiring.Assign(controller, ("_cargoHatch", hatch));
            BuildWiring.AssignArray(controller, "_hatchCrates", HatchCrates(hatch));
            BuildWiring.AssignArray(controller, "_sideBins", new Object[]
            {
                Mount(scratch, sideBin, BuildWiring.Node(m, RoverModelNodes.BinSocketLeft)).gameObject,
                Mount(scratch, sideBin, BuildWiring.Node(m, RoverModelNodes.BinSocketRight)).gameObject,
            });

            RoverPortrait portrait = BuildPortrait(scratch, visual, m, rigTuning);
            BuildWiring.Assign(controller, ("_portrait", portrait));
            SetLayer(instance.transform, Layers.Rover);
            SetLayer(lampMotes.transform, DefaultLayer);

            BuildWiring.Assign(bodyLanguage,
                ("_tuning", characterTuning),
                ("_rover", controller),
                ("_rig", rig),
                ("_neck", BuildWiring.Node(m, RoverModelNodes.Neck)),
                ("_head", BuildWiring.Node(m, RoverModelNodes.Head)),
                ("_eyelid", BuildWiring.Node(m, RoverModelNodes.Eyelid)),
                ("_solarWing", BuildWiring.Node(m, RoverModelNodes.SolarWing)),
                ("_eyeRenderer", BuildWiring.NodeComponent<MeshRenderer>(m, RoverModelNodes.Eye)),
                ("_antennaTipRenderer", BuildWiring.NodeComponent<MeshRenderer>(m, RoverModelNodes.AntennaTip)),
                ("_eyeLight", eyeLight));

            return root;
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
        /// Mounts art's HoverCoils on CoilSocket with identity, hidden (RoverHoverCoils shows it once 07 owns the
        /// Hover-Jump), plus the charge light beside it.
        /// </summary>
        private static RoverHoverCoils BuildHoverCoils(BuilderScratchScene scratch, GameObject host, Transform socket,
            GameObject prefab, RoverRigTuning tuning)
        {
            Transform mount = scratch.Instantiate(prefab, socket).transform;
            mount.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            mount.localScale = Vector3.one;
            var springs = new Object[RoverModelNodes.CoilCount];
            var glows = new Object[RoverModelNodes.CoilCount];
            for (int i = 0; i < springs.Length; i++)
            {
                springs[i] = BuildWiring.Node(mount, RoverModelNodes.Coil(i));
                glows[i] = BuildWiring.NodeComponent<MeshRenderer>(mount, RoverModelNodes.CoilGlow(i));
            }

            mount.gameObject.SetActive(false);

            var light = Child(scratch, "CoilGlow", socket).AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Palette.Get(PaletteSwatch.TechGlow);
            light.range = tuning.HoverCoils.LightRange;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.enabled = false;

            var hoverCoils = host.AddComponent<RoverHoverCoils>();
            BuildWiring.Assign(hoverCoils, ("_tuning", tuning), ("_mount", mount), ("_light", light));
            BuildWiring.AssignArray(hoverCoils, "_coils", springs);
            BuildWiring.AssignArray(hoverCoils, "_glows", glows);
            return hoverCoils;
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
                ("_dustSocketLeft", BuildWiring.Node(model, RoverModelNodes.DustSocketLeft)),
                ("_dustSocketRight", BuildWiring.Node(model, RoverModelNodes.DustSocketRight)),
                ("_trackLeft", Track(scratch, "TrackLeft", host.transform, trackMaterial)),
                ("_trackRight", Track(scratch, "TrackRight", host.transform, trackMaterial)),
                ("_dustLeft", Dust(scratch, "DustLeft", host.transform, dustMaterial)),
                ("_dustRight", Dust(scratch, "DustRight", host.transform, dustMaterial)),
                ("_landingDust", LandingRing(scratch, host.transform, dustMaterial)));
            return wheelFx;
        }

        /// <summary>
        /// 07's visible kit (art's pieces on their sockets with identity, hidden until owned) and the friends' gift
        /// nodes already in RoverModel, wired to <see cref="RoverKit"/> with the road light it now owns.
        /// </summary>
        private static RoverKit BuildKit(BuilderScratchScene scratch, GameObject host, Transform model, Light headlamp,
            RoverRigTuning tuning, GameObject lampBarPrefab, GameObject drumPrefab, GameObject rackPrefab,
            GameObject spinePrefab, GameObject deckPrefab, GameObject ringPrefab, GameObject tubePrefab,
            GameObject[] partPrefabs)
        {
            Transform lampBar = Mount(scratch, lampBarPrefab, BuildWiring.Node(model, RoverModelNodes.HeadlampSocket));
            var lamps = new Object[RoverModelNodes.KitLampCount];
            for (int i = 0; i < lamps.Length; i++)
            {
                lamps[i] = BuildWiring.NodeComponent<MeshRenderer>(lampBar, RoverModelNodes.KitLamp(i));
            }

            Transform drumLeft = Mount(scratch, drumPrefab, BuildWiring.Node(model, RoverModelNodes.DrumSocketLeft));
            Transform drumRight = Mount(scratch, drumPrefab, BuildWiring.Node(model, RoverModelNodes.DrumSocketRight));
            Transform rack = Mount(scratch, rackPrefab, BuildWiring.Node(model, RoverModelNodes.CargoSocket));
            Transform spine = Mount(scratch, spinePrefab, BuildWiring.Node(model, RoverModelNodes.SpineSocket));
            Transform deck = Mount(scratch, deckPrefab, BuildWiring.Node(model, RoverModelNodes.DeckSocket));
            Transform ring = Mount(scratch, ringPrefab, BuildWiring.Node(model, RoverModelNodes.RingSocket));
            Transform tubeSocket = BuildWiring.Node(model, RoverModelNodes.TubeSocket);
            Transform tube = Mount(scratch, tubePrefab, tubeSocket);
            var parts = new Object[partPrefabs.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                Transform part = Mount(scratch, partPrefabs[i], tubeSocket);
                part.name = partPrefabs[i].name;
                parts[i] = part;
            }

            var kit = host.AddComponent<RoverKit>();
            BuildWiring.Assign(kit,
                ("_tuning", tuning),
                ("_headlamp", headlamp),
                ("_lampBar", lampBar),
                ("_cargoRack", rack),
                ("_spine", spine),
                ("_tapeDeck", deck),
                ("_tetherRing", ring),
                ("_ringGlow", BuildWiring.NodeComponent<MeshRenderer>(ring, RoverModelNodes.RingGlow)),
                ("_relicSeat", BuildWiring.Node(rack, RoverModelNodes.RelicSeat)),
                ("_solarCell", BuildWiring.Node(model, RoverModelNodes.CellFilled)),
                ("_freshSerial", BuildWiring.Node(model, RoverModelNodes.Decal07Fresh)),
                ("_pennant", BuildWiring.Node(model, RoverModelNodes.Pennant)),
                ("_tube", tube));
            BuildWiring.AssignArray(kit, "_lamps", lamps);
            BuildWiring.AssignArray(kit, "_storyParts", parts);
            BuildWiring.AssignArray(kit, "_drums", new Object[] { drumLeft, drumRight });
            BuildWiring.AssignArray(kit, "_drumGlows", new Object[]
            {
                BuildWiring.NodeComponent<MeshRenderer>(drumLeft, RoverModelNodes.DrumGlow),
                BuildWiring.NodeComponent<MeshRenderer>(drumRight, RoverModelNodes.DrumGlow),
            });
            return kit;
        }

        /// <summary>
        /// The charge-up's Cells (0.5.5 §5.4): every ConduitGlow renderer (the deck pair and the neck's), the
        /// StageGlow_n sleeve of each Stage_n_L and Stage_n_R (stage 1 first), and the eye's LockGlow.
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
                    Transform cell = BuildWiring.Node(model, RoverModelNodes.StageCell(stage, side == 0));
                    sleeves[2 * (stage - 1) + side] =
                        BuildWiring.NodeComponent<Renderer>(cell, RoverModelNodes.StageGlow(stage));
                }
            }

            BuildWiring.AssignArray(controller, "_conduitGlows", conduits.ToArray());
            BuildWiring.AssignArray(controller, "_stageSleeves", sleeves);
            BuildWiring.Assign(controller,
                ("_lockGlow", BuildWiring.NodeComponent<Renderer>(model, RoverModelNodes.LockGlow)));
        }

        /// <summary>The hatch window's HatchCrate_0, HatchCrate_1, ... nodes in order; at least one, no gaps.</summary>
        private static Object[] HatchCrates(Transform hatch)
        {
            Transform[] nodes = hatch.GetComponentsInChildren<Transform>(true);
            var crates = new List<Object>();
            for (int i = 0; ; i++)
            {
                Transform crate = Find(nodes, RoverModelNodes.HatchCrate(i));
                if (crate == null)
                {
                    break;
                }

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
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = tuning.MoteCount / tuning.MoteLifetime;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.ConeVolume;
            shape.angle = tuning.MoteSpread;
            shape.radius = MoteLensRadius;
            shape.radiusThickness = 1f;
            shape.length = tuning.MoteReach;
            shape.randomDirectionAmount = 1f;

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = tuning.MoteWander;
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

        /// <summary>Rolling dust: soft puffs kicked up and back that pop, settle and drift (rate at runtime).</summary>
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
            drag.enabled = true;
            drag.drag = UnitDrag;
            return dust;
        }

        /// <summary>Landing ring: puffs pushed outward along the ground, lifted gently; emitted on demand.</summary>
        private static ParticleSystem LandingRing(BuilderScratchScene scratch, Transform parent, Material material)
        {
            ParticleSystem ring = CreateSystem(scratch, "LandingDust", parent, material, LandingMaxParticles, false);

            ParticleSystem.ShapeModule shape = ring.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = LandingRingRadius;
            shape.radiusThickness = 0f;
            shape.arc = 360f;

            ParticleSystem.VelocityOverLifetimeModule lift = ring.velocityOverLifetime;
            lift.enabled = true;
            lift.space = ParticleSystemSimulationSpace.Local;
            lift.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            lift.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            lift.z = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);

            ParticleSystem.LimitVelocityOverLifetimeModule drag = ring.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = UnitDrag;
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
                new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1.2f), new Keyframe(1f, 2f)));

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
