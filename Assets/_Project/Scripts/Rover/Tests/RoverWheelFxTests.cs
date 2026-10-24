using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// EditMode verification of 07's vacuum-physics wheel dust particle settings (lunar gravity 1.62 m/s^2,
    /// zero atmospheric drag, no upward drift, non-billowing size curve, short ballistic arc back to ground, warm grey
    /// ground tint, and emission rate response to stillness, speed, acceleration, hard turns).
    /// </summary>
    public sealed class RoverWheelFxTests
    {
        private const float LunarGravity = 1.62f;

        private static readonly string[] DustSystemNames =
        {
            "DustLeft",
            "DustRight",
            "LandingDust",
        };

        [Test]
        public void DustSystems_ObeyVacuumPhysics_LunarGravityZeroDragNoUpwardDrift()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<RoverFxTuning>(RoverAssetPaths.FxTuning);
            Assert.IsNotNull(tuning, $"{RoverAssetPaths.FxTuning} missing.");
            Assert.AreEqual(1f, tuning.DustGravity, 1e-4f, "DustGravity must be 1.0 of lunar gravity (1.62 m/s^2).");
            Assert.AreEqual(1.62f, tuning.DustGravity * LunarGravity, 1e-3f,
                "Effective dust gravity must equal lunar gravity 1.62 m/s^2.");
            Assert.AreEqual(0f, tuning.DustDrag, 1e-5f, "Rolling dust drag must be 0 in vacuum.");
            Assert.AreEqual(0f, tuning.LandingDrag, 1e-5f, "Landing dust drag must be 0 in vacuum.");

            using (var scratch = new BuilderScratchScene())
            {
                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                Transform wheelFx = BuildWiring.Node(rover.transform, "WheelFx");
                foreach (string name in DustSystemNames)
                {
                    ParticleSystem ps = BuildWiring.Node(wheelFx, name).GetComponent<ParticleSystem>();
                    Assert.AreEqual(1f, ps.main.gravityModifier.constant, 1e-4f,
                        $"{name} must fall under full lunar gravity (1.62 m/s^2).");
                    Assert.IsFalse(ps.limitVelocityOverLifetime.enabled,
                        $"{name} must not enable limitVelocityOverLifetime air drag in vacuum.");
                    Assert.AreEqual(0f, ps.limitVelocityOverLifetime.drag.constant, 1e-5f,
                        $"{name} drag constant must be 0.");
                    Assert.IsFalse(ps.velocityOverLifetime.enabled,
                        $"{name} must not have upward or lateral velocityOverLifetime drift.");
                    Assert.IsFalse(ps.forceOverLifetime.enabled,
                        $"{name} must not have external forceOverLifetime drift.");
                    Assert.IsFalse(ps.noise.enabled,
                        $"{name} must not wander with Perlin turbulence in vacuum.");
                }
            }
        }

        [Test]
        public void DustSystems_SizeOverLifetimeNeverBillowsOrSwells()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                Transform wheelFx = BuildWiring.Node(rover.transform, "WheelFx");
                foreach (string name in DustSystemNames)
                {
                    ParticleSystem ps = BuildWiring.Node(wheelFx, name).GetComponent<ParticleSystem>();
                    Assert.IsTrue(ps.sizeOverLifetime.enabled, $"{name} sizeOverLifetime should be enabled.");
                    AnimationCurve curve = ps.sizeOverLifetime.size.curve;
                    float startSize = curve.Evaluate(0f);
                    Assert.AreEqual(1f, startSize, 1e-3f, $"{name} size curve should start at 1.0.");

                    float prev = startSize;
                    for (int step = 1; step <= 20; step++)
                    {
                        float t = step / 20f;
                        float current = curve.Evaluate(t);
                        Assert.LessOrEqual(current, prev + 1e-4f,
                            $"{name} sizeOverLifetime must never swell/billow (t={t:0.00}: {current:0.000} > {prev:0.000}).");
                        prev = current;
                    }

                    Assert.Less(curve.Evaluate(1f), startSize,
                        $"{name} grains must taper smaller as they fall back to the ground.");
                }
            }
        }

        [Test]
        public void DustSystems_ColourIsWarmGreyMatchingGround_NotBlueWhite()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<RoverFxTuning>(RoverAssetPaths.FxTuning);
            Assert.IsNotNull(tuning, $"{RoverAssetPaths.FxTuning} missing.");

            AssertWarmGroundGrey(tuning.DustColor, "DustColor");
            AssertWarmGroundGrey(tuning.DustShadowColor, "DustShadowColor");

            using (var scratch = new BuilderScratchScene())
            {
                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                Transform wheelFx = BuildWiring.Node(rover.transform, "WheelFx");
                foreach (string name in DustSystemNames)
                {
                    ParticleSystem ps = BuildWiring.Node(wheelFx, name).GetComponent<ParticleSystem>();
                    ParticleSystem.MinMaxGradient startColor = ps.main.startColor;
                    AssertWarmGroundGrey(startColor.colorMin, $"{name}.colorMin");
                    AssertWarmGroundGrey(startColor.colorMax, $"{name}.colorMax");
                }
            }
        }

        [Test]
        public void WheelDust_ShortLifetimeCompletesBallisticArcBackToGround()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<RoverFxTuning>(RoverAssetPaths.FxTuning);
            Assert.IsNotNull(tuning, $"{RoverAssetPaths.FxTuning} missing.");
            Assert.LessOrEqual(tuning.DustLifetime, 0.60f,
                "Wheel dust lifetime must be short so grains fall back to the ground instead of hanging.");

            using (var scratch = new BuilderScratchScene())
            {
                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                Transform wheelFx = BuildWiring.Node(rover.transform, "WheelFx");
                foreach (string name in new[] { "DustLeft", "DustRight" })
                {
                    ParticleSystem ps = BuildWiring.Node(wheelFx, name).GetComponent<ParticleSystem>();
                    float elevationDeg = 180f + ps.shape.rotation.x;
                    Assert.That(elevationDeg, Is.InRange(8f, 24f),
                        $"{name} cone elevation should be a low arc behind the contact patch.");

                    float speed = ps.main.startSpeed.constantMax;
                    float lifetime = ps.main.startLifetime.constantMax;
                    float vy0 = speed * Mathf.Sin(elevationDeg * Mathf.Deg2Rad);
                    float g = LunarGravity * ps.main.gravityModifier.constant;
                    float timeToGround = 2f * vy0 / g;
                    Assert.LessOrEqual(timeToGround, lifetime + 0.02f,
                        $"{name} ballistic arc must return to ground level within its lifetime.");
                }
            }
        }

        [Test]
        public void ComputeWheelDustRate_ZeroWhenStill_MoreWhenAcceleratingOrTurningHard()
        {
            var tuning = ScriptableObject.CreateInstance<RoverFxTuning>();
            try
            {
                Assert.AreEqual(0f, RoverWheelFx.ComputeWheelDustRate(tuning, true, 0f, 0f, 0f), 1e-5f,
                    "Still rover must raise zero wheel dust.");
                Assert.AreEqual(0f, RoverWheelFx.ComputeWheelDustRate(tuning, true, 0f, 0f, 2.5f), 1e-5f,
                    "Still rover with chassis settling acceleration must raise zero wheel dust.");
                Assert.AreEqual(0f, RoverWheelFx.ComputeWheelDustRate(tuning, false, 5f, 45f, 2f), 1e-5f,
                    "Airborne rover must raise zero wheel dust.");

                float cruiseRate = RoverWheelFx.ComputeWheelDustRate(tuning, true, 3.5f, 0f, 0f);
                Assert.Greater(cruiseRate, 0f, "Cruising rover must raise wheel dust.");

                float accelRate = RoverWheelFx.ComputeWheelDustRate(tuning, true, 3.5f, 0f, tuning.DustAccelFull);
                Assert.Greater(accelRate, cruiseRate * 1.25f,
                    "Accelerating must kick up noticeably more dust than steady cruising at the same speed.");

                float hardTurnRate = RoverWheelFx.ComputeWheelDustRate(tuning, true, 3.5f, tuning.DustTurnFullYawRate, 0f);
                Assert.Greater(hardTurnRate, cruiseRate * 1.3f,
                    "Turning hard must kick up noticeably more dust than driving straight at the same speed.");

                float pivotTurnRate = RoverWheelFx.ComputeWheelDustRate(tuning, true, 0f, 75f, 0f);
                Assert.Greater(pivotTurnRate, 0f, "Hard pivot turn in place must kick up scrub dust.");
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        private static void AssertWarmGroundGrey(Color color, string label)
        {
            Assert.Greater(color.r, color.b,
                $"{label} ({color}) must be warm grey (R > B), never blue-white.");
            Assert.GreaterOrEqual(color.r, color.g,
                $"{label} ({color}) must have warm red/tan balance (R >= G).");
            float luma = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
            Assert.That(luma, Is.InRange(0.28f, 0.56f),
                $"{label} luminance ({luma:0.000}) must match regolith ground albedo, not bright white.");
        }
    }
}
