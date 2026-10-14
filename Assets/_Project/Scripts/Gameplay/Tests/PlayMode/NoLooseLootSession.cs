using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Real-scene PlayMode verification of 0.5.5 acceptance 1 (<c>docs/design/0.5.5-owner-feedback.md</c> §2 and §7):
    /// "nothing glowing or pickable on bare regolith". Boots the built <c>Main.unity</c> scene on the playthrough's
    /// isolated save slot, waits until 07 wakes and every runtime spawner (<see cref="SalvageField"/>,
    /// <see cref="RelicField"/>, <see cref="ExcavationSystem"/>, <see cref="FriendField"/>,
    /// <see cref="CassetteField"/>, <see cref="LogCacheField"/>, <see cref="RelayField"/>,
    /// <see cref="TowerPartField"/>, <see cref="GaragePort"/>, <see cref="ScanField"/>, <see cref="CrewCrates"/>,
    /// <see cref="SealedContainers"/>, <see cref="CargoBundles"/>, <see cref="SonarSystem"/>, and
    /// <see cref="BellSignalField"/>) has initialised and stepped, captures 1920x1080 URP post-processed PNGs for every
    /// pose in <c>Assets/_Project/Scripts/Gameplay/Tests/PlayMode/capture_sweep_poses.json</c> into
    /// <c>Logs/no-loose-loot/</c>, and then asserts that every pickable, diggable, cuttable, scannable, or glowing lure
    /// object in the world is hosted by a place and never standing alone on bare terrain.
    /// <para>
    /// <b>Hosting rule (defined from the runtime code):</b>
    /// An interactive or glowing object in the world is hosted if and only if it belongs to one of the seven place
    /// kinds below:
    /// <list type="number">
    /// <item>
    /// <b>Salvage site (<see cref="SalvageField.Sites"/> and <see cref="SalvageField.ScrapFieldId"/>):</b> a
    /// <see cref="SalvageSite"/> root (<c>Site_depot</c>, <c>Site_kestrel</c>, <c>Site_drill</c>, <c>Site_garage</c>,
    /// <c>Site_lander</c>) or its flat anchor footprint (<see cref="SalvageSite.Radius"/>), or one of Kestrel-3's four
    /// half-buried hull fragment plates (<c>Kestrel3_ScrapField/Fragment_0..3</c>). Hosts its attached/loose
    /// <see cref="SalvagePiece"/>s and their cut seams, its <see cref="CrewCrate"/> on <c>CrateSocket</c>, its buried
    /// <see cref="Relic"/>s in <c>RelicPocket_&lt;id&gt;/DigSpot</c>, its <see cref="ScanFragment"/>s on
    /// <c>FragmentSocket_*</c>, its heart containers (<c>CoilCrate</c>, <c>JunctionBox</c>, <c>SpareCrate</c>), and its
    /// crew tin (<c>LogCache_*</c>).
    /// </item>
    /// <item>
    /// <b>Derelict (<see cref="SalvageField.Derelicts"/>, <c>Jumper_Wreck</c>, or a broken <see cref="Friend"/> body at
    /// its camp/crash site):</b> a robot derelict root (<c>Derelict_06</c> with its <c>RelayHut</c>,
    /// <c>Derelict_Mule</c>, <c>Derelict_HaulBot</c>, <c>Derelict_Drone</c>), Kenji's crashed test jumper
    /// (<c>Jumper_Wreck</c> at <c>canyon.lip</c>), or a broken friend's camp within its site radius
    /// (<c>Friend_tilly</c> at Tilly's crater, <c>Friend_bell</c> at <c>canyon.terminus</c>). Hosts its
    /// <see cref="SalvagePiece"/>s, its <see cref="ScanFragment"/>s on <c>FragmentSocket_*</c>, Ro's <c>TapeCase</c> on
    /// <c>Derelict_06/TapeCaseSocket</c>, and Ro's <c>TapeCase</c> and <c>LogCache_ro_1</c> beside Bell at
    /// <c>canyon.terminus</c>.
    /// </item>
    /// <item>
    /// <b>Container (<see cref="SealedContainers"/>, <see cref="CrewCrate"/>, or <c>LogCache_*</c> tin box):</b> a
    /// <see cref="SealedContainer"/> (<c>CoilCrate</c>, <c>JunctionBox</c>, <c>SpareCrate</c>, <c>TapeCase</c>,
    /// <c>SupplyPod</c>, <c>TillySled/Clamp</c>, <c>TillySled/SensorPod</c>, <c>Relay_*/Broken/Cabinet</c>), a site's
    /// <see cref="CrewCrate"/>, or a <c>LogCache_*</c> tin box, provided the container itself stands at a salvage site,
    /// derelict, sled, relay mast, friend camp, or is a landed <c>SupplyPod</c>. Hosts the pickup or glint resting on
    /// its <c>ItemSocket</c> / open mouth.
    /// </item>
    /// <item>
    /// <b>The sled (<c>Friend_tilly/TillySled</c>):</b> Tilly's crashed survey sled (<see cref="Friend.InSled"/>),
    /// hosting her three parts at <c>PartSocket_0</c> (sealed in <c>Clamp</c>), <c>PartSocket_1</c> (half-buried under
    /// the tipped hull, <see cref="Friend.BuriedPart"/>), and <c>SensorPod/PartSocket_2</c> (sealed in
    /// <c>SensorPod/Lid</c>).
    /// </item>
    /// <item>
    /// <b>Relay cabinet (<see cref="RelayMast.Cabinet"/>):</b> the sealed service cabinet on each mast's
    /// <c>CabinetSocket</c>, hosting the mast's <c>Part_RelayModule</c> and the tower's <c>Part_RelaySpare</c>.
    /// </item>
    /// <item>
    /// <b>Home base (<see cref="HomeBase"/> / <c>[Gameplay]/Base</c>):</b> the lander, charging dock (<c>DockGlow</c>),
    /// <c>HomeHalo</c>, <see cref="RadioTower"/>, <see cref="Workshop"/> (<c>RoverBay</c>),
    /// <see cref="BlueprintPlotter"/>, <c>CrewLift</c>, <see cref="CassetteShelf"/>, and <see cref="HomeShelves"/>.
    /// </item>
    /// <item>
    /// <b>Crew-built structure (<see cref="RelayField"/> masts, <see cref="GaragePort"/>, <c>SupplyPod</c>):</b> the
    /// four <c>Relay_relay.0..3</c> masts and their <c>RelayPad_*</c> pads, Kenji's <c>GaragePort</c> and
    /// <c>GaragePortPad</c> in <c>site.garage</c>, and the fallen <c>SupplyPod</c> (with its <c>Glow</c> beacon) at
    /// <c>canyon.ledge</c>.
    /// </item>
    /// </list>
    /// Any pickable part, dig target, cut seam, scan fragment, held bundle, container, or active pickup glint / beacon
    /// that is not hosted by one of these seven places is recorded as an offender with its name, world position, and
    /// spawner.
    /// </para>
    /// </summary>
    [Explicit("Real-scene PlayMode check that nothing pickable or glowing lies on bare regolith (0.5.5 §2, §7).")]
    [Category("NoLooseLoot")]
    public sealed class NoLooseLootSession
    {
        private const string PosesPath = "Assets/_Project/Scripts/Gameplay/Tests/PlayMode/capture_sweep_poses.json";
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 0.5f;
        private const int DefaultWidth = 1920;
        private const int DefaultHeight = 1080;
        private const float HostMargin = 1.5f;
        private const float HullSocketTolerance = 0.5f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "no-loose-loot"));

        private readonly List<string> _problems = new List<string>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private IDisposable _awoke;
        private bool _awake;

        [SetUp]
        public void SetUp()
        {
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            _awoke?.Dispose();
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator EveryFindAndGlint_IsHostedByAPlace_AndSweepsAreCaptured()
        {
            yield return Boot();

            Directory.CreateDirectory(CaptureFolder);
            yield return CaptureAllPoses();

            var offenders = new List<string>();
            VerifyFriendParts(offenders);
            VerifyCassettes(offenders);
            VerifyLogCaches(offenders);
            VerifyRelicsAndPockets(offenders);
            VerifyContainersAndSeals(offenders);
            VerifyTowerAndGarageParts(offenders);
            VerifyRelayPartsAndCabinets(offenders);
            VerifyScanFragments(offenders);
            VerifySalvageAndBundles(offenders);
            VerifyBeaconsAndHalos(offenders);
            yield return CaptureListenNearHome();

            Assert.IsEmpty(_problems, "Unexpected errors in the Unity log:\n" + string.Join("\n", _problems));
            if (offenders.Count > 0)
            {
                string summary = $"Found {offenders.Count} unhosted item(s)/glint(s) on bare regolith (0.5.5 §2, §7):\n"
                                 + string.Join("\n", offenders);
                Assert.Fail(summary);
            }
        }

        private IEnumerator Boot()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("NoLooseLootSession loads PlaythroughScene through the Unity Editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "Main scene has GameBootstrap");
            Assert.IsNotNull(_gameplay, "Main scene has GameplaySystem");
            _context = bootstrap.Context;
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float deadline = Time.time + WakeTimeout;
            while (!_awake && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_awake, "07 woke up in PlayMode");
            yield return new WaitForSeconds(SettleSeconds);
            yield return null;
        }

        /// <summary>
        /// Renders every pose in <see cref="PosesPath"/> plus diagnostic close-ups of Tilly's sled and Bell's
        /// three canyon alcoves at 1920x1080 with the game camera's URP post-processing. Before each shot, the view
        /// camera is moved to the pose's eye for one frame while 07 stays on the home pad, so <see cref="PickupGlints"/>
        /// evaluates its distance fade and camera-facing quads from the pose's eye.
        /// </summary>
        private IEnumerator CaptureAllPoses()
        {
            Assert.IsTrue(File.Exists(PosesPath), $"Sweep poses file not found at '{PosesPath}'.");
            PoseFile file = JsonUtility.FromJson<PoseFile>(File.ReadAllText(PosesPath));
            Assert.IsNotNull(file, "Parsed sweep poses JSON");
            int width = file.width > 0 ? file.width : DefaultWidth;
            int height = file.height > 0 ? file.height : DefaultHeight;

            var allPoses = new List<Pose>(file.poses ?? Array.Empty<Pose>());
            AppendDiagnosticPoses(allPoses);

            IViewCamera viewCamera = _context.Get<IViewCamera>();
            Camera view = viewCamera.Camera;
            Vector3 savedPosition = view.transform.position;
            Quaternion savedRotation = view.transform.rotation;
            float savedFov = view.fieldOfView;
            var disabled = new List<Behaviour>();
            if (viewCamera is Behaviour rigBehaviour && rigBehaviour.enabled)
            {
                rigBehaviour.enabled = false;
                disabled.Add(rigBehaviour);
            }

            foreach (Behaviour behaviour in view.GetComponents<Behaviour>())
            {
                if (behaviour != null && behaviour.enabled && !(behaviour is Camera) &&
                    !(behaviour is AudioListener) && !(behaviour is UniversalAdditionalCameraData))
                {
                    behaviour.enabled = false;
                    disabled.Add(behaviour);
                }
            }

            try
            {
                for (int i = 0; i < allPoses.Count; i++)
                {
                    Pose pose = allPoses[i];
                    var eye = new Vector3(pose.position[0], pose.position[1], pose.position[2]);
                    var look = new Vector3(pose.lookAt[0], pose.lookAt[1], pose.lookAt[2]);
                    float fov = pose.fov > 0f ? pose.fov : 60f;
                    Quaternion rotation = Quaternion.LookRotation(look - eye);
                    view.transform.SetPositionAndRotation(eye, rotation);
                    view.fieldOfView = fov;
                    yield return null;

                    string outPath = Path.Combine(CaptureFolder, pose.name + ".png");
                    Review(view, eye, look, fov, width, height, outPath);
                }
            }
            finally
            {
                for (int i = 0; i < disabled.Count; i++)
                {
                    if (disabled[i] != null)
                    {
                        disabled[i].enabled = true;
                    }
                }

                view.transform.SetPositionAndRotation(savedPosition, savedRotation);
                view.fieldOfView = savedFov;
            }

            yield return null;
        }

        private void AppendDiagnosticPoses(List<Pose> poses)
        {
            Friend tilly = _gameplay.Friends.Find("tilly");
            if (tilly != null)
            {
                Transform sled = _gameplay.Friends.transform.Find("Friend_tilly/" + ContainerKinds.TillySled);
                if (sled != null)
                {
                    Vector3 target = sled.position + Vector3.up * 0.25f;
                    Vector3 eye = sled.position - sled.forward * 2.8f + sled.right * 1.6f + Vector3.up * 1.4f;
                    poses.Add(MakePose("16_tilly_sled_close", eye, target, 55f));
                }
            }

            Friend bell = _gameplay.Friends.Find("bell");
            IWorldAnchors anchors = _context.Get<IWorldAnchors>();
            if (bell != null)
            {
                for (int p = 0; p < bell.Parts.Length; p++)
                {
                    Vector3 partPos = bell.PartRest[p];
                    Vector3 intoAlcove = Vector3.forward;
                    if (anchors.TryGet(WorldAnchorIds.CanyonAlcovePrefix + p, out WorldAnchor alcove))
                    {
                        intoAlcove = new Vector3(alcove.Forward.x, 0f, alcove.Forward.z).normalized;
                    }

                    Vector3 eye = partPos - intoAlcove * 5.2f + Vector3.up * 1.4f;
                    poses.Add(MakePose($"17_bell_alcove_{p}_close", eye, partPos, 55f));
                }
            }
        }

        private static Pose MakePose(string name, Vector3 eye, Vector3 lookAt, float fov)
        {
            return new Pose
            {
                name = name,
                position = new[] { eye.x, eye.y, eye.z },
                lookAt = new[] { lookAt.x, lookAt.y, lookAt.z },
                fov = fov,
            };
        }

        private static void Review(Camera view, Vector3 eye, Vector3 look, float fov, int width, int height,
            string path)
        {
            var host = new GameObject("NoLooseLootReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                UniversalAdditionalCameraData source = view.GetUniversalAdditionalCameraData();
                UniversalAdditionalCameraData target = camera.GetUniversalAdditionalCameraData();
                if (source != null && target != null)
                {
                    target.renderPostProcessing = source.renderPostProcessing;
                    target.antialiasing = source.antialiasing;
                    target.antialiasingQuality = source.antialiasingQuality;
                    target.renderShadows = source.renderShadows;
                    target.dithering = source.dithering;
                }

                var lamp = host.AddComponent<Light>();
                lamp.type = LightType.Spot;
                lamp.range = 16f;
                lamp.spotAngle = 80f;
                lamp.innerSpotAngle = 45f;
                lamp.intensity = 3.2f;
                lamp.color = new Color(1f, 0.95f, 0.88f);
                lamp.shadows = LightShadows.None;

                host.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
                camera.fieldOfView = fov;
                FrameCapture.SavePng(camera, width, height, path);
            }
            finally
            {
                Object.Destroy(host);
            }
        }

        /// <summary>
        /// Triggers one listen near home with answers at ~30 m (<c>site.depot</c>), ~80 m (<c>Friend_tilly</c>), and
        /// ~130 m (<c>site.garage</c>) and captures 1920x1080 frames at the peak of each answer's blink into
        /// <c>Logs/captures/</c>.
        /// </summary>
        private IEnumerator CaptureListenNearHome()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));
            Directory.CreateDirectory(folder);
            ITerrainQuery terrain = _context.Get<ITerrainQuery>();
            var roverComponent = _context.Get<IRoverState>() as Component;
            Assert.IsNotNull(roverComponent, "IRoverState is a Component");
            Transform roverTransform = roverComponent.transform;
            var roverBody = roverComponent.GetComponent<Rigidbody>();
            Vector3 savedRoverPos = roverTransform.position;
            Quaternion savedRoverRot = roverTransform.rotation;
            bool wasKinematic = roverBody != null && roverBody.isKinematic;

            IViewCamera viewCamera = _context.Get<IViewCamera>();
            Camera view = viewCamera.Camera;
            Vector3 savedCamPos = view.transform.position;
            Quaternion savedCamRot = view.transform.rotation;
            float savedFov = view.fieldOfView;
            var disabled = new List<Behaviour>();
            if (viewCamera is Behaviour rigBehaviour && rigBehaviour.enabled)
            {
                rigBehaviour.enabled = false;
                disabled.Add(rigBehaviour);
            }

            foreach (Behaviour behaviour in view.GetComponents<Behaviour>())
            {
                if (behaviour != null && behaviour.enabled && !(behaviour is Camera) &&
                    !(behaviour is AudioListener) && !(behaviour is UniversalAdditionalCameraData))
                {
                    behaviour.enabled = false;
                    disabled.Add(behaviour);
                }
            }

            try
            {
                if (roverBody != null)
                {
                    roverBody.linearVelocity = Vector3.zero;
                    roverBody.angularVelocity = Vector3.zero;
                    roverBody.isKinematic = true;
                }

                var origin = new Vector3(-6.5f, terrain.SampleHeight(-6.5f, 29f) + 0.35f, 29f);
                roverTransform.SetPositionAndRotation(origin, Quaternion.identity);
                yield return null;

                SalvageSite depot = FindSite("site.depot");
                Friend tilly = _gameplay.Friends.Find("tilly");
                SalvageSite garage = FindSite("site.garage");
                AnswerBlink tillyBlink = _gameplay.Sonar.Blinks[_gameplay.Relics.Relics.Count];

                int pingCount = 0;
                using IDisposable pingSub = _context.Events.Subscribe<SonarPinged>(_ => pingCount++);
                _gameplay.Power.Restore(1);
                float pingDeadline = Time.time + _gameplay.Sonar.Tuning.RestSeconds + 2f;
                while (pingCount == 0 && Time.time < pingDeadline)
                {
                    yield return null;
                }

                Assert.Greater(pingCount, 0, "07 listened once near home");

                yield return WaitAndFrameBlink(depot.AnswerPosition, () => depot.AnswerBlinkIntensity > 0.4f,
                    origin, terrain, roverTransform, view, Path.Combine(folder, "listen_30m_depot.png"));
                yield return WaitAndFrameBlink(tilly.AnswerPosition, () => tillyBlink.Intensity > 0.4f,
                    origin, terrain, roverTransform, view, Path.Combine(folder, "listen_80m_tilly.png"));
                yield return WaitAndFrameBlink(garage.AnswerPosition, () => garage.AnswerBlinkIntensity > 0.4f,
                    origin, terrain, roverTransform, view, Path.Combine(folder, "listen_130m_garage.png"));
            }
            finally
            {
                _gameplay.Power.Restore(0);
                if (roverBody != null)
                {
                    roverBody.isKinematic = wasKinematic;
                }

                roverTransform.SetPositionAndRotation(savedRoverPos, savedRoverRot);
                for (int i = 0; i < disabled.Count; i++)
                {
                    if (disabled[i] != null)
                    {
                        disabled[i].enabled = true;
                    }
                }

                view.transform.SetPositionAndRotation(savedCamPos, savedCamRot);
                view.fieldOfView = savedFov;
            }
        }

        private static IEnumerator WaitAndFrameBlink(Vector3 target, Func<bool> blinking, Vector3 origin,
            ITerrainQuery terrain, Transform roverTransform, Camera view, string outPath)
        {
            float deadline = Time.time + 8f;
            while (!blinking() && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(blinking(), $"Answer blink appeared for {Path.GetFileName(outPath)}");
            yield return new WaitForSeconds(0.25f);

            Vector3 dir = target - origin;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward;
            roverTransform.SetPositionAndRotation(origin, Quaternion.LookRotation(dir));
            Vector3 eye = origin - dir * 5.2f + Vector3.up * 2.2f;
            eye.y = Mathf.Max(eye.y, terrain.SampleHeight(eye.x, eye.z) + 2f);
            view.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
            view.fieldOfView = 55f;
            yield return null;
            FrameCapture.SavePng(view, DefaultWidth, DefaultHeight, outPath);
        }

        private SalvageSite FindSite(string id)
        {
            for (int i = 0; i < _gameplay.Salvage.Sites.Count; i++)
            {
                if (_gameplay.Salvage.Sites[i].Id == id)
                {
                    return _gameplay.Salvage.Sites[i];
                }
            }

            throw new InvalidOperationException($"Missing salvage site '{id}'.");
        }

        /// <summary>
        /// Checks every friend's parts (<see cref="FriendField"/>). A friend part is hosted only if it is sealed inside
        /// a <see cref="SealedContainer"/> (<c>friend.PartContainers[p] != null</c>), rests in/under Tilly's crashed
        /// survey sled (<c>friend.InSled</c> at <c>TillySled/PartSocket_*</c>), or rests buried in a
        /// <c>RockfallPocket</c> (<c>friend.PartPockets[p]/DigSpot</c>). Parts hovering on open ground with no
        /// container, sled, or pocket (and emitting <see cref="PickupGlints"/> on bare terrain) are offenders.
        /// </summary>
        private void VerifyFriendParts(List<string> offenders)
        {
            ITerrainQuery terrain = _context.Get<ITerrainQuery>();
            Transform friendsRoot = _gameplay.Friends.transform;
            for (int f = 0; f < _gameplay.Friends.Friends.Count; f++)
            {
                Friend friend = _gameplay.Friends.Friends[f];
                Transform friendRoot = friendsRoot.Find("Friend_" + friend.Id);
                Transform sled = friendRoot != null ? friendRoot.Find(ContainerKinds.TillySled) : null;
                for (int p = 0; p < friend.Parts.Length; p++)
                {
                    Transform part = friend.Parts[p];
                    Vector3 pos = part.position;
                    SealedContainer box = friend.PartContainers[p];
                    if (box != null)
                    {
                        if (!IsContainerHosted(box, out string boxHost))
                        {
                            offenders.Add(Format(
                                "Friend part '{0}' (friend '{1}', part {2}) at {3}: its container '{4}' is unhosted ({5}).",
                                HierarchyPath(part), friend.Id, p, Vec(pos), box.Id, boxHost));
                        }

                        continue;
                    }

                    if (friend.InSled && sled != null)
                    {
                        Transform hullSocket = sled.Find("PartSocket_" + p);
                        if (hullSocket != null &&
                            Vector3.Distance(friend.PartRest[p], hullSocket.position) <= HullSocketTolerance)
                        {
                            continue;
                        }
                    }

                    Transform pocket = friend.PartPockets[p];
                    if (pocket != null && friend.IsPartBuried(p))
                    {
                        Transform digSpot = pocket.Find("DigSpot");
                        if (digSpot != null &&
                            Vector3.Distance(friend.PartRest[p], digSpot.position) <= HullSocketTolerance)
                        {
                            continue;
                        }
                    }

                    float groundY = terrain.SampleHeight(pos.x, pos.z);
                    offenders.Add(Format(
                        "Friend part '{0}' (friend '{1}', part '{2}' [{3}]) at world {4} (rest {5}, {6:F2} m above " +
                        "bare regolith y={7:F2}) — spawned by FriendField.Spawn / FriendAnchorPlanner.Plan with " +
                        "InSled=false, PartContainers[{3}]=null, PartPockets[{3}]=null, and active PickupGlints on " +
                        "bare regolith; breaks docs/design/0.5.5-owner-feedback.md §2 lines 171-173 & §7 line 501.",
                        HierarchyPath(part), friend.Id, friend.Definition.Parts[p].Id, p, Vec(pos),
                        Vec(friend.PartRest[p]), pos.y - groundY, groundY));
                }
            }
        }

        /// <summary>
        /// Checks every cassette pickup (<see cref="CassetteField"/>): each must rest inside a hosted
        /// <see cref="SealedContainer"/> (<c>TapeCase</c> or <c>SupplyPod</c>) and remain sealed (inactive, un-glinting)
        /// at boot.
        /// </summary>
        private void VerifyCassettes(List<string> offenders)
        {
            for (int i = 0; i < _gameplay.Cassettes.Count; i++)
            {
                CassetteDefinition def = _gameplay.Cassettes.Definition(i);
                SealedContainer container = _gameplay.Cassettes.Container(i);
                Transform pickup = _gameplay.Cassettes.transform.Find("Cassette_" + def.Id);
                Vector3 pos = pickup != null ? pickup.position : _gameplay.Cassettes.Site(i).Position;
                if (container == null)
                {
                    offenders.Add(Format(
                        "Cassette '{0}' at {1} — spawned by CassetteField with no SealedContainer (0.5.5 §2).",
                        def.Id, Vec(pos)));
                    continue;
                }

                if (_gameplay.Cassettes.IsWaiting(i) || (pickup != null && pickup.gameObject.activeInHierarchy))
                {
                    offenders.Add(Format(
                        "Cassette '{0}' at {1} — exposed/glinting before container '{2}' is cut open (0.5.5 §2).",
                        def.Id, Vec(pos), container.Id));
                }

                if (!IsContainerHosted(container, out string reason))
                {
                    offenders.Add(Format(
                        "Cassette container '{0}' ({1}) for tape '{2}' at {3} — not hosted by a place ({4}).",
                        container.Id, container.Kind, def.Id, Vec(container.Root.position), reason));
                }
            }
        }

        /// <summary>
        /// Checks every crew log cache (<see cref="LogCacheField"/>): each <c>LogCache_&lt;id&gt;</c> tin box and its
        /// interior glint must stand inside a salvage site footprint or beside a broken friend at her camp.
        /// </summary>
        private void VerifyLogCaches(List<string> offenders)
        {
            for (int i = 0; i < _gameplay.Logs.Count; i++)
            {
                LogCacheDefinition def = _gameplay.Logs.Definition(i);
                Vector3 pos = _gameplay.Logs.Position(i);
                Transform tin = _gameplay.Logs.transform.Find("LogCache_" + def.LogId);
                if (tin == null)
                {
                    offenders.Add(Format("LogCache '{0}' at {1} has no tin box visual under LogCaches.",
                        def.LogId, Vec(pos)));
                    continue;
                }

                if (!IsPositionAtHostedPlace(pos, out _))
                {
                    offenders.Add(Format(
                        "LogCache 'LogCache_{0}' at {1} (anchor '{2}') — tin box and its PickupGlint stand outside " +
                        "every salvage site, derelict, friend camp, and structure.",
                        def.LogId, Vec(pos), def.Anchor.AnchorId));
                }
            }
        }

        /// <summary>
        /// Checks every relic (<see cref="RelicField"/>) and excavation target (<see cref="ExcavationSystem"/>): each
        /// relic must rest in <c>RelicPocket_&lt;id&gt;/DigSpot</c> under its <see cref="SalvageSite.Root"/>, with its
        /// halo off (<c>HaloLevel == 0</c>) while buried.
        /// </summary>
        private void VerifyRelicsAndPockets(List<string> offenders)
        {
            foreach (Relic relic in _gameplay.Relics.Relics)
            {
                Vector3 pos = relic.transform.position;
                if (relic.Home == null || relic.Home.Root == null)
                {
                    offenders.Add(Format("Relic '{0}' at {1} — has no host SalvageSite.",
                        relic.Definition.Id, Vec(pos)));
                    continue;
                }

                Transform digSpot = relic.Home.Root.Find("RelicPocket_" + relic.Definition.Id + "/DigSpot");
                if (digSpot == null)
                {
                    offenders.Add(Format(
                        "Relic '{0}' at {1} — host site '{2}' has no RelicPocket_{0}/DigSpot node (0.5.5 §2).",
                        relic.Definition.Id, Vec(pos), relic.Home.Id));
                    continue;
                }

                if (SurfaceRules.HorizontalDistance(pos, digSpot.position) > 0.25f)
                {
                    offenders.Add(Format(
                        "Relic '{0}' at {1} — displaced from its wreck pocket '{2}' at {3}.",
                        relic.Definition.Id, Vec(pos), digSpot.name, Vec(digSpot.position)));
                }

                if (relic.State != RelicState.Buried || relic.HaloLevel > 0.01f)
                {
                    offenders.Add(Format(
                        "Relic '{0}' at {1} — State={2}, HaloLevel={3:F2} at boot (expected Buried with no halo).",
                        relic.Definition.Id, Vec(pos), relic.State, relic.HaloLevel));
                }
            }
        }

        /// <summary>
        /// Checks every <see cref="SealedContainer"/> in <see cref="SealedContainers"/> and every site's
        /// <see cref="CrewCrate"/>: each container and its glowing seal must be hosted by a salvage site, derelict,
        /// sled, relay mast, friend camp, or be the landed <c>SupplyPod</c> structure.
        /// </summary>
        private void VerifyContainersAndSeals(List<string> offenders)
        {
            for (int i = 0; i < _gameplay.Containers.Count; i++)
            {
                SealedContainer container = _gameplay.Containers.Get(i);
                if (!IsContainerHosted(container, out string reason))
                {
                    offenders.Add(Format(
                        "SealedContainer '{0}' ({1}, root '{2}') at {3} (seal at {4}) — unhosted ({5}).",
                        container.Id, container.Kind, container.Root.name, Vec(container.Root.position),
                        Vec(container.CutPoint.position), reason));
                }
            }

            foreach (SalvageSite site in _gameplay.Salvage.Sites)
            {
                CrewCrate crate = site.Crate;
                if (crate == null || crate.Root == null || !crate.Root.IsChildOf(site.Root))
                {
                    Vector3 pos = crate?.Root != null ? crate.Root.position : site.Position;
                    offenders.Add(Format("CrewCrate for '{0}' at {1} — not parented under site root '{2}'.",
                        site.Id, Vec(pos), site.Root.name));
                }
            }
        }

        /// <summary>
        /// Checks the radio tower's three world components (<see cref="TowerPartField"/>) and Kenji's garage port parts
        /// (<see cref="GaragePort"/>): each must be sealed inside its container or cabinet at boot, never resting or
        /// glinting in the open.
        /// </summary>
        private void VerifyTowerAndGarageParts(List<string> offenders)
        {
            TowerParts parts = _gameplay.TowerParts;
            TowerPartField field = _gameplay.TowerPartField;
            for (int i = 0; i < parts.Count; i++)
            {
                TowerPart part = parts.PartAt(i);
                Transform model = _gameplay.transform.Find(part.PartId);
                if (field.IsResting(i) || (model != null && model.gameObject.activeInHierarchy))
                {
                    Vector3 pos = model != null ? model.position : field.RestPosition(i);
                    offenders.Add(Format(
                        "Tower part '{0}' at {1} — active/glinting at boot before its container or cabinet is opened.",
                        part.PartId, Vec(pos)));
                }

                if (part.Source == TowerPartSource.SiteHeart)
                {
                    SealedContainer box = _gameplay.Containers.Find(TowerPartField.ContainerId(part));
                    if (box == null)
                    {
                        offenders.Add(Format(
                            "Tower part '{0}' at site '{1}' has no SealedContainer (0.5.5 §2).",
                            part.PartId, part.SiteId));
                    }
                }
            }

            GaragePort port = _gameplay.Port;
            Transform deck = _gameplay.transform.Find("TapeDeck");
            if (port.SpareCrate == null || !IsContainerHosted(port.SpareCrate, out _))
            {
                offenders.Add(Format("GaragePort SpareCrate at {0} is missing or unhosted.",
                    port.SpareCrate != null ? Vec(port.SpareCrate.Root.position) : "NONE"));
            }

            if (port.DeckInTheCrate || port.DeckOnTheTray || (deck != null && deck.gameObject.activeInHierarchy))
            {
                offenders.Add(Format("GaragePort TapeDeck at {0} is exposed before SpareCrate is cut open.",
                    Vec(port.DeckPosition)));
            }
        }

        /// <summary>
        /// Checks every relay mast's spare part and service cabinet (<see cref="RelayField"/>): each mast's
        /// <c>Part_RelayModule</c> must rest sealed inside <see cref="RelayMast.Cabinet"/> on the mast's foot.
        /// </summary>
        private void VerifyRelayPartsAndCabinets(List<string> offenders)
        {
            foreach (RelayMast mast in _gameplay.Relays.Masts)
            {
                Transform mastGroup = mast.Broken?.Root != null ? mast.Broken.Root.parent : null;
                if (mast.Cabinet == null || mastGroup == null || !mast.Cabinet.Root.IsChildOf(mastGroup))
                {
                    offenders.Add(Format(
                        "Relay '{0}' at {1} — service cabinet is missing or not mounted under Relay_{0}.",
                        mast.Id, Vec(mast.Anchor.Position)));
                    continue;
                }

                if (mast.PartState != RelayPartState.Resting || mast.Cabinet.StandsOpen ||
                    Vector3.Distance(mast.Part.position, mast.Cabinet.ItemSocket.position) > 0.25f)
                {
                    offenders.Add(Format(
                        "Relay part '{0}' for '{1}' at {2} — state={3}, cabinetOpen={4}, socketDist={5:F2}m.",
                        mast.Part.name, mast.Id, Vec(mast.Part.position), mast.PartState,
                        mast.Cabinet.StandsOpen, Vector3.Distance(mast.Part.position, mast.Cabinet.ItemSocket.position)));
                }
            }
        }

        /// <summary>
        /// Checks every blueprint scan fragment (<see cref="ScanField.Fragments"/>): each must be parented to a
        /// <c>FragmentSocket_*</c> node under a salvage site root, derelict root, or <c>Jumper_Wreck</c>.
        /// </summary>
        private void VerifyScanFragments(List<string> offenders)
        {
            foreach (ScanFragment fragment in _gameplay.Scans.Fragments)
            {
                Transform socket = fragment.Root != null ? fragment.Root.parent : null;
                if (socket == null || !socket.name.StartsWith(ScanFragment.SocketPrefix, StringComparison.Ordinal) ||
                    !IsTransformOnHostStructure(socket, out _))
                {
                    Vector3 pos = fragment.Root != null ? fragment.Root.position : fragment.Point;
                    offenders.Add(Format(
                        "ScanFragment '{0}' (host '{1}') at {2} — not mounted on a hosted FragmentSocket_* node.",
                        fragment.FragmentId, fragment.HostId, Vec(pos)));
                }
            }
        }

        /// <summary>
        /// Checks every salvage piece (<see cref="SalvageField.Pieces"/>) and waiting cargo bundle
        /// (<see cref="CargoBundles"/>): each piece must belong to a salvage site, derelict, or Kestrel-3 hull
        /// fragment, with no loose bundle waiting on bare ground.
        /// </summary>
        private void VerifySalvageAndBundles(List<string> offenders)
        {
            foreach (SalvagePiece piece in _gameplay.Salvage.Pieces)
            {
                if (piece.Site == null || piece.Site.Root == null || !piece.Body.IsChildOf(piece.Site.Root))
                {
                    offenders.Add(Format("SalvagePiece '{0}' at {1} — not parented under a SalvageSite/Derelict root.",
                        piece.Body != null ? piece.Body.name : "null", Vec(piece.CutPosition)));
                }

                if (piece.Bundle != null && piece.Bundle.gameObject.activeInHierarchy)
                {
                    offenders.Add(Format("SalvagePiece bundle '{0}' at {1} — active in a fresh game.",
                        piece.Bundle.name, Vec(piece.Bundle.position)));
                }
            }

            for (int i = 0; i < _gameplay.Bundles.Count; i++)
            {
                Vector3 pos = _gameplay.Bundles.Position(i);
                if (!IsPositionAtHostedPlace(pos, out _))
                {
                    offenders.Add(Format("CargoBundle[{0}] at {1} — waiting on bare ground outside any wreck or crate.",
                        i, Vec(pos)));
                }
            }
        }

        /// <summary>
        /// Checks world beacons and halos (<c>SupplyPod/Glow</c>, <see cref="SonarSystem"/>, and
        /// <see cref="BellSignalField"/>): verifies that the <c>SupplyPod</c> and its beacon are at <c>canyon.ledge</c>
        /// (and not left at world origin <c>(0, 0, 0)</c> on the home pad as in EditMode), and that no sonar or signal
        /// pillar is lit on bare terrain at boot.
        /// </summary>
        private void VerifyBeaconsAndHalos(List<string> offenders)
        {
            Transform supplyPod = _gameplay.transform.Find(ContainerKinds.SupplyPod);
            if (supplyPod != null)
            {
                if (SurfaceRules.HorizontalDistance(supplyPod.position, Vector3.zero) < 10f)
                {
                    offenders.Add(Format(
                        "SupplyPod at {0} — still standing near world origin (0, 0, 0) on the home pad.",
                        Vec(supplyPod.position)));
                }

                if (_context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.CanyonLedge, out WorldAnchor ledge) &&
                    SurfaceRules.HorizontalDistance(supplyPod.position, ledge.Position) > ledge.Radius + HostMargin)
                {
                    offenders.Add(Format("SupplyPod at {0} — not at canyon.ledge {1}.",
                        Vec(supplyPod.position), Vec(ledge.Position)));
                }
            }

            if (_gameplay.Signals.Pillar != null && _gameplay.Signals.Pillar.Level > 0.001f)
            {
                offenders.Add(Format("BellSignalField pillar is lit (level {0:F2}) at {1} in a fresh game.",
                    _gameplay.Signals.Pillar.Level, Vec(_gameplay.Signals.Signals.TargetPosition)));
            }
        }

        private bool IsContainerHosted(SealedContainer container, out string detail)
        {
            if (container == null || container.Root == null)
            {
                detail = "container or its Root is null";
                return false;
            }

            if (container.Kind == ContainerKind.Pod &&
                string.Equals(container.Root.name, ContainerKinds.SupplyPod, StringComparison.Ordinal))
            {
                detail = "SupplyPod structure";
                return true;
            }

            if (IsTransformOnHostStructure(container.Root, out detail))
            {
                return true;
            }

            return IsPositionAtHostedPlace(container.Root.position, out detail);
        }

        private bool IsTransformOnHostStructure(Transform node, out string hostName)
        {
            for (int i = 0; i < _gameplay.Salvage.Sites.Count; i++)
            {
                SalvageSite site = _gameplay.Salvage.Sites[i];
                if (site.Root != null && node.IsChildOf(site.Root))
                {
                    hostName = site.Id;
                    return true;
                }
            }

            for (int i = 0; i < _gameplay.Salvage.Derelicts.Count; i++)
            {
                SalvageSite derelict = _gameplay.Salvage.Derelicts[i];
                if (derelict.Root != null && node.IsChildOf(derelict.Root))
                {
                    hostName = derelict.Id;
                    return true;
                }
            }

            Transform jumper = _gameplay.transform.Find("JumperWreck") ?? _gameplay.transform.Find("Jumper_Wreck");
            if (jumper != null && node.IsChildOf(jumper))
            {
                hostName = "JumperWreck (canyon.lip)";
                return true;
            }

            Transform sled = _gameplay.Friends.transform.Find("Friend_tilly/" + ContainerKinds.TillySled);
            if (sled != null && (node == sled || node.IsChildOf(sled)))
            {
                hostName = "Friend_tilly/TillySled";
                return true;
            }

            for (int i = 0; i < _gameplay.Relays.Masts.Count; i++)
            {
                RelayMast mast = _gameplay.Relays.Masts[i];
                Transform mastGroup = mast.Broken?.Root != null ? mast.Broken.Root.parent : null;
                if ((mastGroup != null && node.IsChildOf(mastGroup)) ||
                    (mast.Broken?.Root != null && node.IsChildOf(mast.Broken.Root)) ||
                    (mast.Restored?.Root != null && node.IsChildOf(mast.Restored.Root)))
                {
                    hostName = mast.Id;
                    return true;
                }
            }

            Transform baseRoot = _gameplay.transform.Find("Base");
            if (baseRoot != null && node.IsChildOf(baseRoot))
            {
                hostName = "HomeBase";
                return true;
            }

            hostName = "not parented under any site, derelict, jumper, sled, relay mast, or base root";
            return false;
        }

        private bool IsPositionAtHostedPlace(Vector3 position, out string hostName)
        {
            for (int i = 0; i < _gameplay.Salvage.Sites.Count; i++)
            {
                SalvageSite site = _gameplay.Salvage.Sites[i];
                if (SurfaceRules.HorizontalDistance(position, site.Position) <= site.Radius + HostMargin ||
                    SurfaceRules.HorizontalDistance(position, site.Heart) <= site.Radius + HostMargin)
                {
                    hostName = site.Id;
                    return true;
                }
            }

            for (int i = 0; i < _gameplay.Salvage.Derelicts.Count; i++)
            {
                SalvageSite derelict = _gameplay.Salvage.Derelicts[i];
                if (derelict.Id == SalvageField.ScrapFieldId)
                {
                    for (int p = 0; p < derelict.Pieces.Count; p++)
                    {
                        if (SurfaceRules.HorizontalDistance(position, derelict.Pieces[p].Body.position) <= 3f)
                        {
                            hostName = $"{derelict.Id}/Fragment_{p}";
                            return true;
                        }
                    }

                    continue;
                }

                if (SurfaceRules.HorizontalDistance(position, derelict.Position) <= derelict.Radius + HostMargin)
                {
                    hostName = derelict.Id;
                    return true;
                }
            }

            for (int i = 0; i < _gameplay.Friends.Friends.Count; i++)
            {
                Friend friend = _gameplay.Friends.Friends[i];
                float radius = _gameplay.Friends.Tuning != null ? _gameplay.Friends.Tuning.RepairRadius : 4f;
                if (SurfaceRules.HorizontalDistance(position, friend.Site.Position) <= radius + HostMargin)
                {
                    hostName = $"Friend_{friend.Id} site";
                    return true;
                }
            }

            for (int i = 0; i < _gameplay.Relays.Masts.Count; i++)
            {
                RelayMast mast = _gameplay.Relays.Masts[i];
                if (SurfaceRules.HorizontalDistance(position, mast.Anchor.Position) <= mast.Anchor.Radius + HostMargin)
                {
                    hostName = mast.Id;
                    return true;
                }
            }

            if (SurfaceRules.HorizontalDistance(position, _gameplay.Home.LanderPosition) <= 20f)
            {
                hostName = "HomeBase";
                return true;
            }

            hostName = "bare regolith outside every salvage site, derelict, sled, relay mast, friend camp, and base";
            return false;
        }

        private static string HierarchyPath(Transform t)
        {
            if (t == null)
            {
                return "<null>";
            }

            var parts = new List<string>();
            for (Transform cur = t; cur != null; cur = cur.parent)
            {
                parts.Add(cur.name);
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Vec(Vector3 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F2}, {1:F2}, {2:F2})", v.x, v.y, v.z);
        }

        private static string Format(string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add($"{type}: {message}");
            }
        }

        [Serializable]
        private sealed class PoseFile
        {
            public int width = DefaultWidth;
            public int height = DefaultHeight;
            public Pose[] poses = Array.Empty<Pose>();
        }

        [Serializable]
        private sealed class Pose
        {
            public string name = string.Empty;
            public float[] position = Array.Empty<float>();
            public float[] lookAt = Array.Empty<float>();
            public float fov = 60f;
        }
    }
}
