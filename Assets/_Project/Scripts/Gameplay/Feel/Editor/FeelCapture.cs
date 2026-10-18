using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.Art.Editor;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Editor.Automation;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Feel.Editor
{
    /// <summary>
    /// Captures each beat of the cut's look at 1080p with the game's post (docs/design/feel-pass.md, Acceptance P0):
    /// charge, ignition, sustain, crack, tumble, fold and received; then polish wave 1's beats and the scan's close
    /// (the ribbon into 07's lens, the lens flare). Opens Main.unity (never saved), sets 07 before a
    /// depot wreck and drives the feel kit with a scripted beat publisher, so it needs no gameplay.
    /// <code>
    /// python tools/unity_batch.py exec --method MoonProject.Gameplay.Feel.Editor.FeelCapture.Capture
    /// </code>
    /// PNGs land in the run's output folder under feel/.
    /// </summary>
    public static class FeelCapture
    {
        private const float Step = 1f / 60f;
        private const float ChargeSeconds = 0.65f;
        private const float CutSeconds = 3f;
        private const float ActInterval = 0.25f;
        private const float TumbleSeconds = 1.2f;
        private const float TumbleRise = 0.5f;
        private const float TumbleTurn = 70f;
        private const float FoldSeconds = 0.6f;
        private const float StandOff = 2.4f;
        private const float CutHalfLength = 0.35f;
        private const float Probe = 200f;
        private const float SeamDistance = 1.3f;
        private const float WaveSettle = 2.5f;
        private const float LeapShot = 0.12f;
        private const float LandShot = 0.35f;
        private const float TetherShot = 0.5f;
        private const float TetherLift = 1.2f;
        private const float AlignSeconds = 0.4f;
        private const float SweepShot = 0.3f;
        private const float ScanReach = 2.6f;
        private const float RibbonShot = 0.55f;
        private const float DissolveSeconds = 0.9f;
        private const float ChaseDistance = 7.5f;

        // Built by Gameplay/Tuning (GameplayAssetPaths.ScanTuning, which this editor assembly does not reference).
        private const string ScanTuningPath = "Assets/_Project/Data/Tuning/Gameplay/ScanTuning.asset";
        private const float ChasePitch = 16f;
        private const float ChaseHeight = 1.1f;
        private const float ChaseFov = 52f;
        private static readonly Vector3 Spot = new Vector3(-26f, 0f, -18f);

        public static void Capture()
        {
            BatchRunner.Run(nameof(Capture), args =>
            {
                // Opening the scene unloads assets nothing in it references, so load ours after it opens.
                EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
                var tuning = AssetDatabase.LoadAssetAtPath<FeelVfxTuning>(FeelMaterials.TuningPath);
                var visuals = AssetDatabase.LoadAssetAtPath<GameplayVisuals>(FeelMaterials.VisualsPath);
                var scans = AssetDatabase.LoadAssetAtPath<ScanTuning>(ScanTuningPath);
                string broken = tuning == null ? "the feel tuning is missing (run Gameplay/FeelVfx)"
                    : scans == null ? "the scan tuning is missing (run Gameplay/Tuning)"
                    : visuals == null ? "GameplayVisuals is missing (run Gameplay/Materials)" : visuals.Validate();
                if (broken != null)
                {
                    Debug.LogError($"FeelCapture: {broken}");
                    return false;
                }

                var temporary = new List<Object>();
                try
                {
                    return Run(tuning, scans, visuals, Path.Combine(args.OutputDirectory, "feel"), temporary) > 0;
                }
                finally
                {
                    foreach (Object item in temporary)
                    {
                        Object.DestroyImmediate(item);
                    }
                }
            });
        }

        private static int Run(FeelVfxTuning tuning, ScanTuning scans, GameplayVisuals visuals, string output,
            List<Object> temporary)
        {
            HideSceneRover();
            Vector3 ground = Ground(Spot);
            GameObject site = Spawn(ArtPaths.SitesFolder + "/Site_depot.prefab", temporary);
            site.transform.SetPositionAndRotation(ground, Quaternion.identity);
            Transform piece = FirstPiece(site.transform);
            Transform cut = piece.Find(SiteModelBuilder.CutPointName);
            Vector3 normalLocal = cut.localRotation * Vector3.forward;
            Vector3 cutWorld = cut.position;
            Vector3 outward = Vector3.ProjectOnPlane(piece.TransformDirection(normalLocal), Vector3.up).normalized;
            GameObject rover = Spawn(ArtPaths.R1ModelsFolder + "/Rover/" + RoverModelBuilder.ModelName + ".prefab", temporary);
            rover.transform.SetPositionAndRotation(Ground(cutWorld + outward * StandOff),
                Quaternion.LookRotation(-outward, Vector3.up));
            Transform emitter = Descendant(rover.transform, "CutterOrigin");
            var anchors = new FeelCaptureAnchors(emitter, piece, cut.localPosition, normalLocal, CutHalfLength);
            var host = new GameObject("FeelCaptureHost");
            temporary.Add(host);
            var events = new EventBus();
            var vfx = new FeelVfx(host.transform, tuning, visuals, events, anchors, scans);
            var shots = new Shots(output);
            float clock = 0f;

            Publish(events, FeelPhase.Anticipate, anchors, 0f, ChargeSeconds);
            clock = Run(vfx, clock, ChargeSeconds * 0.7f);
            shots.Take("01_charge", Close(emitter, rover.transform));
            shots.Take("01b_charge_chase", Chase(rover.transform));
            clock = Run(vfx, clock, ChargeSeconds * 0.3f);
            Publish(events, FeelPhase.Act, anchors, 0f, 0f);
            clock = Run(vfx, clock, Step * 2f);
            shots.Take("02_ignition", Wide(emitter.position, anchors.CutPoint, rover.transform, 0.7f));
            for (float cutTime = ActInterval; cutTime <= CutSeconds; cutTime += ActInterval)
            {
                anchors.Progress = cutTime / CutSeconds;
                Publish(events, FeelPhase.Act, anchors, anchors.Progress, 0f);
                clock = Run(vfx, clock, ActInterval);
                if (Mathf.Abs(cutTime - CutSeconds * 0.65f) < ActInterval * 0.5f)
                {
                    shots.Take("03_sustain", Wide(emitter.position, anchors.CutPoint, rover.transform, 1f));
                    shots.Take("03b_seam", FaceOn(anchors));
                }
            }

            Publish(events, FeelPhase.Impact, anchors, 1f, 0f);
            clock = Run(vfx, clock, Step * 3f);
            shots.Take("04_crack", Wide(emitter.position, anchors.CutPoint, rover.transform, 1f));
            Publish(events, FeelPhase.Follow, anchors, 1f, TumbleSeconds);
            Vector3 rest = piece.position;
            Quaternion restTurn = piece.rotation;
            for (float t = 0f; t < TumbleSeconds; t += Step)
            {
                float u = t / TumbleSeconds;
                piece.SetPositionAndRotation(rest + Vector3.up * TumbleRise * Mathf.Sin(u * Mathf.PI * 0.5f),
                    restTurn * Quaternion.Euler(TumbleTurn * u, TumbleTurn * 0.4f * u, 0f));
                clock = Run(vfx, clock, Step);
                if (Mathf.Abs(u - 0.55f) < Step / TumbleSeconds)
                {
                    shots.Take("05_tumble", Wide(emitter.position, piece.position, rover.transform, 1f));
                }
            }

            Publish(events, FeelPhase.Follow, anchors, 1f, FoldSeconds);
            piece.gameObject.SetActive(false);
            string bundleName = SiteModelBuilder.BundleName(SalvagePieceMaterial.Metal);
            GameObject bundle = Spawn(ArtPaths.PickupFolder + "/" + bundleName + ".prefab", temporary);
            bundle.transform.position = piece.position;
            clock = Run(vfx, clock, FoldSeconds);
            clock = Run(vfx, clock, Step);
            shots.Take("06_fold", Close(bundle.transform, rover.transform));
            bundle.transform.position = Descendant(rover.transform, "CargoSocket").position;
            Publish(events, FeelPhase.Reward, anchors, 1f, 0f);
            Run(vfx, clock, Step * 4f);
            shots.Take("07_received", Wide(rover.transform.position + Vector3.up, anchors.CutPoint, rover.transform,
                1.2f));
            CaptureWaveOne(events, vfx, rover.transform, shots);
            CaptureScan(events, vfx, rover.transform, shots, tuning, temporary);
            vfx.Dispose();
            return shots.Written;
        }

        /// <summary>
        /// Polish wave 1's beats (docs/design/polish-pass.md §3.2): leap, landing, tether dust, install.
        /// </summary>
        private static void CaptureWaveOne(EventBus events, FeelVfx vfx, Transform rover, Shots shots)
        {
            Vector3 feet = rover.position;
            Run(vfx, 0f, WaveSettle);
            Vector3 coils = Descendant(rover, "CoilSocket").position;
            events.Publish(new FeelBeat(FeelKind.Leap, FeelPhase.Act, coils, Vector3.up, 1f, 0f));
            Run(vfx, 0f, LeapShot);
            shots.Take("08_leap", Wide(feet + Vector3.up, feet + rover.forward * 2f, rover, 1.2f));
            Run(vfx, 0f, WaveSettle);
            events.Publish(new FeelBeat(FeelKind.Land, FeelPhase.Impact, feet, Vector3.up, 1f, 0f));
            Run(vfx, 0f, LandShot);
            shots.Take("09_land", Wide(feet + Vector3.up, feet + rover.forward * 2f, rover, 1.2f));
            Run(vfx, 0f, WaveSettle);
            Vector3 lifted = feet + rover.forward * 2.2f + Vector3.up * TetherLift;
            events.Publish(new FeelBeat(FeelKind.Tether, FeelPhase.Follow, lifted, Vector3.up, 1f, 0f));
            Run(vfx, 0f, TetherShot);
            shots.Take("10_tether", Wide(feet + Vector3.up, lifted, rover, 1f));
            Vector3 socket = Descendant(rover, "CargoSocket").position;
            events.Publish(new FeelBeat(FeelKind.Install, FeelPhase.Anticipate, socket, rover.up, 0f, AlignSeconds));
            Run(vfx, 0f, AlignSeconds);
            shots.Take("11_install_align", Above(socket, rover));
            events.Publish(new FeelBeat(FeelKind.Install, FeelPhase.Act, socket, -rover.forward, 1f, SweepShot * 2f));
            Run(vfx, 0f, SweepShot);
            shots.Take("12_install_sweep", Above(socket, rover));
        }

        /// <summary>
        /// The scan's close (0.5.5 §1.2, D2): a fragment before 07 dissolves, its lines stream into the lens as a
        /// ribbon and the lens flares. The fragment's _Dissolve is driven here as gameplay will, in step with the beat.
        /// </summary>
        private static void CaptureScan(EventBus events, FeelVfx vfx, Transform rover, Shots shots,
            FeelVfxTuning tuning, List<Object> temporary)
        {
            Run(vfx, 0f, WaveSettle);
            GameObject fragment = Spawn(ArtPaths.FragmentFolder + "/" + FragmentBuilder.PrefabName("FocusLens", 0)
                + ".prefab", temporary);
            fragment.transform.SetPositionAndRotation(Ground(rover.position + rover.forward * ScanReach),
                rover.rotation * Quaternion.Euler(0f, 180f, 0f));
            Transform target = Descendant(fragment.transform, FragmentBuilder.ScanTargetName);
            var block = new MaterialPropertyBlock();
            Renderer[] renderers =
            {
                Descendant(fragment.transform, FragmentBuilder.SolidName).GetComponent<Renderer>(),
                Descendant(fragment.transform, FragmentBuilder.LinesName).GetComponent<Renderer>(),
            };
            events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Reward, target.position, Vector3.up, 1f,
                DissolveSeconds));
            float at = 0f;
            foreach ((string name, float until) in new[]
            {
                ("13_scan_ribbon", tuning.ScanRibbonDelay + tuning.ScanRibbonSeconds * RibbonShot),
                ("14_scan_flare", tuning.ScanRibbonDelay + tuning.ScanRibbonSeconds + tuning.ScanFlareSeconds * 0.3f),
            })
            {
                for (; at < until; at += Step)
                {
                    block.SetFloat("_ScanProgress", 1f);
                    block.SetFloat("_Dissolve", Mathf.Clamp01(at / DissolveSeconds));
                    foreach (Renderer renderer in renderers)
                    {
                        renderer.SetPropertyBlock(block);
                    }

                    vfx.Step(Step);
                }

                shots.Take(name, Wide(rover.position + Vector3.up, target.position, rover, 0.9f));
            }
        }

        private static float Run(FeelVfx vfx, float clock, float seconds)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += Step)
            {
                vfx.Step(Step);
                clock += Step;
            }

            return clock;
        }

        private static void Publish(EventBus events, FeelPhase phase, FeelCaptureAnchors anchors, float strength,
            float seconds)
        {
            FeelKind kind = phase == FeelPhase.Reward ? FeelKind.Collect : FeelKind.Cut;
            events.Publish(new FeelBeat(kind, phase, anchors.CutPoint, anchors.CutNormal, strength, seconds));
        }

        /// <summary>Close on <paramref name="subject"/> from 07's right, a little above.</summary>
        private static CameraPose Close(Transform subject, Transform rover)
        {
            Vector3 eye = subject.position + rover.right * 1.1f + rover.forward * 0.8f + Vector3.up * 0.25f;
            return Pose(eye, subject.position + rover.forward * 0.3f, 45f);
        }

        /// <summary>From behind and above 07's back, onto <paramref name="subject"/>.</summary>
        private static CameraPose Above(Vector3 subject, Transform rover)
        {
            return Pose(subject - rover.forward * 1.6f + rover.right * 0.6f + Vector3.up * 1.1f, subject, 45f);
        }

        /// <summary>
        /// The game's default chase camera behind 07 (RoverCameraTuning: 7.5 m out, 16° up, at 1.1 m).
        /// </summary>
        private static CameraPose Chase(Transform rover)
        {
            Vector3 target = rover.position + Vector3.up * ChaseHeight;
            float pitch = ChasePitch * Mathf.Deg2Rad;
            Vector3 back = -rover.forward * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
            return Pose(target + back * ChaseDistance, target, ChaseFov);
        }

        /// <summary>Square onto the cut face, close enough to read the seam cooling behind the hot point.</summary>
        private static CameraPose FaceOn(FeelCaptureAnchors anchors)
        {
            Vector3 seam = anchors.CutPiece.TransformPoint(anchors.CutLine[1]);
            return Pose(seam + anchors.CutNormal * SeamDistance + Vector3.up * 0.2f, seam, 40f);
        }

        /// <summary>From 07's right, wide enough to hold the emitter and <paramref name="target"/>.</summary>
        private static CameraPose Wide(Vector3 from, Vector3 target, Transform rover, float scale)
        {
            Vector3 middle = Vector3.Lerp(from, target, 0.5f);
            Vector3 eye = middle + rover.right * 3.2f * scale - rover.forward * 0.6f * scale + Vector3.up * 0.9f;
            return Pose(eye, middle, 50f);
        }

        private static CameraPose Pose(Vector3 eye, Vector3 look, float fov)
        {
            return new CameraPose
            {
                position = new[] { eye.x, eye.y, eye.z }, lookAt = new[] { look.x, look.y, look.z }, fov = fov,
            };
        }

        private static Vector3 Ground(Vector3 at)
        {
            return Physics.Raycast(new Vector3(at.x, Probe, at.z), Vector3.down, out RaycastHit hit, Probe * 2f)
                ? hit.point
                : throw new InvalidOperationException($"FeelCapture: no ground under {at}.");
        }

        private static GameObject Spawn(string path, List<Object> temporary)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"FeelCapture: {path} is missing; run the Art builders.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            temporary.Add(instance);
            return instance;
        }

        private static Transform FirstPiece(Transform site)
        {
            foreach (Transform child in site)
            {
                if (child.name.StartsWith("Salvage_", StringComparison.Ordinal))
                {
                    return child;
                }
            }

            throw new InvalidOperationException($"FeelCapture: {site.name} has no salvage piece.");
        }

        private static Transform Descendant(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            throw new InvalidOperationException($"FeelCapture: {root.name} has no {name}.");
        }

        private static void HideSceneRover()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "[Rover]")
                {
                    root.SetActive(false);
                }
            }
        }

        /// <summary>One 1080p frame per beat, through the game's post.</summary>
        private sealed class Shots
        {
            private readonly string _output;

            public Shots(string output)
            {
                _output = output;
            }

            public int Written { get; private set; }

            public void Take(string name, CameraPose pose)
            {
                pose.name = name;
                var set = new CameraPoseSet
                {
                    width = 1920, height = 1080, postProcessing = true, poses = new[] { pose },
                };
                Written += CaptureAutomation.CapturePoses(set, _output, "cut");
            }
        }
    }
}
