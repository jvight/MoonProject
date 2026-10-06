using UnityEngine;
using UnityEngine.Rendering;
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
    ///   Visual         RoverVisualRig, RoverHoverCoils
    ///     Chassis      jelly lean / bob
    ///       RoverModel (nested prefab; adds Headlamp spot under HeadlampSocket and EyeGlow point under Eye)
    ///         CoilSocket
    ///           HoverCoils (nested prefab, inactive until 07 owns the Hover-Jump)
    ///           CoilGlow   soft cyan point light, off until the jump charges
    ///   WheelFx        RoverWheelFx: TrackLeft/Right ribbons, DustLeft/Right, LandingDust
    /// </code>
    /// Fails with a clear error when RoverModel.prefab does not exist yet: there is no placeholder model.
    /// </summary>
    public static class RoverPrefabBuilder
    {
        private const int DustMaxParticles = 160;
        private const int LandingMaxParticles = 240;
        private const float DustConeAngle = 30f;
        private const float DustConeRadius = 0.15f;
        private const float LandingRingRadius = 0.7f;

        /// <summary>Dust drifts on the tuned drag; the builder sets a unit curve for it to scale.</summary>
        private const float UnitDrag = 1f;

        /// <summary>How far the lighter half of the puffs is lifted from the dust swatch toward white.</summary>
        private const float DustHighlight = 0.2f;

        /// <summary>Kicks rolling dust up and back from the rear wheel contact (cone axis tipped back).</summary>
        private static readonly Vector3 DustConeRotation = new Vector3(-120f, 0f, 0f);

        [MoonBuilder("Rover/Rover", 310)]
        public static void Build()
        {
            var model = BuildWiring.Require<GameObject>(RoverAssetPaths.RoverModel, "the Art box's rover builder");
            var coils = BuildWiring.Require<GameObject>(RoverAssetPaths.HoverCoils, "the Art box's rover builder");
            var tuning = BuildWiring.Require<RoverTuning>(RoverAssetPaths.RoverTuning, "Rover/Tuning");
            var rigTuning = BuildWiring.Require<RoverRigTuning>(RoverAssetPaths.RigTuning, "Rover/Tuning");
            var fxTuning = BuildWiring.Require<RoverFxTuning>(RoverAssetPaths.FxTuning, "Rover/Tuning");
            var characterTuning =
                BuildWiring.Require<RoverCharacterTuning>(RoverAssetPaths.CharacterTuning, "Rover/Tuning");
            var sphereMaterial =
                BuildWiring.Require<PhysicsMaterial>(RoverAssetPaths.SpherePhysicsMaterial, "Rover/Materials");
            var trackMaterial = BuildWiring.Require<Material>(RoverAssetPaths.TrackMaterial, "Rover/Materials");
            var dustMaterial = BuildWiring.Require<Material>(RoverAssetPaths.DustMaterial, "Rover/Materials");

            using (var scratch = new BuilderScratchScene())
            {
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
                    ("_antenna", BuildWiring.Node(m, RoverModelNodes.Antenna)),
                    ("_headlamp", headlamp));
                BuildWiring.AssignArray(rig, "_wheels", wheels);
                Transform coilSocket = BuildWiring.Node(m, RoverModelNodes.CoilSocket);
                RoverHoverCoils hoverCoils = BuildHoverCoils(scratch, visual, coilSocket, coils, rigTuning);

                RoverWheelFx wheelFx = BuildWheelFx(scratch, root.transform, m, fxTuning, trackMaterial, dustMaterial);

                BuildWiring.Assign(controller,
                    ("_tuning", tuning),
                    ("_body", body),
                    ("_sphere", collider),
                    ("_visualRig", rig),
                    ("_wheelFx", wheelFx),
                    ("_hoverCoils", hoverCoils),
                    ("_tetherOrigin", BuildWiring.Node(m, RoverModelNodes.TetherOrigin)),
                    ("_cargoSocket", BuildWiring.Node(m, RoverModelNodes.CargoSocket)));

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

                GeneratedAssets.SavePrefab(root, RoverAssetPaths.RoverPrefab);
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
