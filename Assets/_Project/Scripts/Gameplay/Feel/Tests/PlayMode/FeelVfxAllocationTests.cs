using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Feel.Tests
{
    /// <summary>
    /// The cut's feel kit costs no garbage per frame (docs/design/feel-pass.md, Acceptance P0) with its pools full:
    /// 64 sparks, 24 slag beads, 24 charge motes and the flashes, all live while it steps.
    /// </summary>
    public sealed class FeelVfxAllocationTests
    {
        private const string AllocatedInFrame = "GC Allocated In Frame";
        private const float Step = 1f / 60f;
        private const int FillSteps = 240;
        private const int BaselineFrames = 6;
        private const int MeasuredFrames = 6;
        private const int StepsPerFrame = 20;
        private const int ControlBytes = 4096;
        private const int ControlCount = 16;
        private const float BeadLifetime = 30f;
        private const int Seed = 7;

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.Destroy(created);
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator FullPools_StepWithoutAllocating()
        {
            UnityEngine.Random.InitState(Seed);
            FeelVfxTuning tuning = Tuning();
            var events = new EventBus();
            var anchors = new Anchors(Make("Emitter", new Vector3(0f, 1.2f, 0f)),
                Make("Piece", new Vector3(0f, 1f, 1.5f)));
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 4f;
            _created.Add(ground);
            var vfx = new FeelVfx(Make("Host", Vector3.zero), tuning, Visuals(tuning), events, anchors);

            Publish(events, FeelPhase.Anticipate, 0f, 0.65f);
            Publish(events, FeelPhase.Act, 0f, 0f);
            Publish(events, FeelPhase.Act, 1f, 0f);
            Publish(events, FeelPhase.Impact, 1f, 0f);
            Publish(events, FeelPhase.Anticipate, 0f, 0.65f);
            Publish(events, FeelPhase.Act, 0f, 0f);
            Publish(events, FeelPhase.Act, 1f, 0f);
            for (int i = 0; i < FillSteps; i++)
            {
                vfx.Step(Step);
            }

            Publish(events, FeelPhase.Anticipate, 0f, 30f);
            vfx.Step(Step);
            Assert.AreEqual(tuning.SparkPool, vfx.Debris.Flying, "the spark pool is full");
            Assert.AreEqual(tuning.BeadPool, vfx.Debris.Resting, "the bead pool is full");
            Assert.AreEqual(tuning.MotePool, vfx.Charge.MotesShown, "the mote pool is full");
            PublishScan(events);
            vfx.Step(Step);
            Assert.AreEqual(tuning.ScanRibbonBeads, vfx.ScanRibbon.Shown, "the scan ribbon streams");

            using (ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, AllocatedInFrame))
            {
                yield return null;
                long noise = 0L;
                for (int i = 0; i < BaselineFrames; i++)
                {
                    yield return null;
                    noise = Math.Max(noise, recorder.LastValue);
                }

                long withFeel = long.MaxValue;
                for (int i = 0; i < MeasuredFrames; i++)
                {
                    PublishScan(events);
                    for (int s = 0; s < StepsPerFrame; s++)
                    {
                        vfx.Step(Step);
                    }

                    yield return null;
                    withFeel = Math.Min(withFeel, recorder.LastValue);
                }

                var control = new object[ControlCount];
                for (int i = 0; i < control.Length; i++)
                {
                    control[i] = new byte[ControlBytes];
                }

                yield return null;
                long withControl = recorder.LastValue;
                GC.KeepAlive(control);
                Debug.Log($"[feel-gc] plain frames <= {noise} B; quietest frame with {StepsPerFrame} feel steps: " +
                          $"{withFeel} B; control frame: {withControl} B");
                Assert.Greater(withControl, noise + ControlCount * ControlBytes / 2,
                    "The frame allocation counter must see a known allocation.");
                Assert.LessOrEqual(withFeel, noise, "stepping the feel kit with full pools allocates nothing");
            }

            vfx.Dispose();
        }

        // A scan's dissolve beginning: the ribbon streams through every measured frame (see Tuning).
        private static void PublishScan(EventBus events)
        {
            events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Reward, new Vector3(0f, 1f, 1.5f), Vector3.up, 1f,
                0f));
        }

        private static void Publish(EventBus events, FeelPhase phase, float strength, float seconds)
        {
            events.Publish(new FeelBeat(FeelKind.Cut, phase, Vector3.zero, Vector3.back, strength, seconds));
        }

        private FeelVfxTuning Tuning()
        {
            var tuning = ScriptableObject.CreateInstance<FeelVfxTuning>();
            _created.Add(tuning);
            Set(tuning, "_moteCount", tuning.MotePool);
            Set(tuning, "_sparkRate", new Vector2(400f, 400f));

            // The ribbon streams from its first step and outlasts a measured frame's steps.
            Set(tuning, "_scanRibbonDelay", 0f);
            Set(tuning, "_scanRibbonSeconds", 1.5f);

            // Beads outlive the test, so once filled the bead pool stays full whatever the timing.
            Set(tuning, "_beadLifetime", new Vector2(BeadLifetime, BeadLifetime));
            return tuning;
        }

        private GameplayVisuals Visuals(FeelVfxTuning tuning)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _created.Add(material);
            var visuals = ScriptableObject.CreateInstance<GameplayVisuals>();
            _created.Add(visuals);
            visuals.PopulateFeel(material, material, material, material, tuning);
            return visuals;
        }

        private Transform Make(string name, Vector3 position)
        {
            var made = new GameObject(name);
            made.transform.position = position;
            _created.Add(made);
            return made.transform;
        }

        private static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"FeelVfxTuning has no {field}");
            info.SetValue(target, value);
        }

        private sealed class Anchors : IFeelAnchors
        {
            private readonly Transform _emitter;
            private readonly Vector3[] _line = { new Vector3(-0.3f, 0f, 0f), new Vector3(0.3f, 0f, 0f) };

            public Anchors(Transform emitter, Transform piece)
            {
                _emitter = emitter;
                CutPiece = piece;
            }

            public Vector3 Emitter => _emitter.position;

            public Vector3 CutPoint => CutPiece.position;

            public Vector3 CutNormal => Vector3.back;

            public Transform CutPiece { get; }

            public IReadOnlyList<Vector3> CutLine => _line;

            public Vector3 CutFaceNormal => Vector3.back;
        }
    }
}
