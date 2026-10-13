using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Gameplay;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// Screenshots of every UI state over the real Main scene (world, rover, sky, real camera), for taste review. The
    /// UI renders into its own target and is composited over the camera's frame. Gameplay state is staged through
    /// <see cref="FakeGameServices"/>; the scene's own save slot is never written (the UI saves to a test slot and the
    /// scene is torn down before play mode ends). The last shots are taken at a narrow 5:4 frame. Needs a GPU, takes a
    /// few minutes: run explicitly with
    /// <c>python tools/unity_batch.py tests --platform playmode --category UiCapture</c>. PNGs land in
    /// Logs/ui-captures.
    /// </summary>
    [Category("UiCapture")]
    [Explicit("Renders the full Main scene; run with --category UiCapture.")]
    public sealed partial class UiCaptureTests : InputTestFixture
    {
        private const string MainScene = "Assets/_Project/Scenes/Main.unity";
        private const int Width = 1600;
        private const int Height = 900;
        private const int NarrowWidth = 1200;
        private const int NarrowHeight = 960;
        private const float WaitLimitSeconds = 30f;
        private const string OutputFolder = "Logs/ui-captures";
        private const float FriendShotDistance = 7f;
        private const float FriendShotHeight = 2.4f;
        private const float FriendShotFov = 50f;
        private const string BellId = "bell";
        private const string BellCornerNode = "BellCorner";
        private const string BellPrefabPath = "Assets/_Project/Generated/Art/Friends/Bell.prefab";
        private const float BellHomeTolerance = 1.5f;
        private const float BellShotFront = 4.2f;
        private const float BellShotSide = 1.6f;
        private const float BellShotHeight = 2f;
        private const float BellLookHeight = 1.1f;
        private const string PartSocketNode = "PartSocket";
        private const float MastSocketReach = 8f;
        private const float MastShotFront = 4.5f;
        private const float MastShotSide = 1.8f;
        private const float MastShotHeight = 1.3f;
        private const float MastLookHeight = 1f;
        private const float HopHold = 0.45f;
        private const float HopMidFade = 0.55f;
        private const string DepotSite = "site.depot";
        private const float SiteShotBack = 9f;
        private const float SiteShotHeight = 3.2f;
        private const float CutHeight = 0.8f;
        private const float CutHeld = 0.55f;
        private const string RelicPrefabPath = "Assets/_Project/Generated/Art/Relics/Relic_cassette_player.prefab";
        private const float RelicShotAhead = 10f;
        private const float RelicShotSide = 2.2f;
        private const float RelicShotYaw = 35f;
        private const float KitSettleSeconds = 1.1f;
        private const float LineShotSeconds = 0.5f;
        private const float TowerStitchDelay = 1.3f;
        private const float SceneSettleSeconds = 4f;
        private const string TillyId = "tilly";
        private const string BellPath = "Assets/_Project/Data/Content/Friends/Friend_bell.asset";
        private const float TillyRise = 1.5f;
        private const float EarthShotReach = 6.5f;
        private const float EarthShotNearest = 2.5f;
        private const float EarthShotStep = 0.25f;
        private const float EarthShotClearance = 0.4f;

        private string _slot;
        private GameObject _uiHost;
        private GameObject _fakesHost;
        private PanelSettings _panel;
        private RenderTexture _target;
        private UiTuning _tuning;
        private GameObject _friendCamera;
        private GameObject _bellCamera;
        private GameObject _bellStandIn;
        private GameObject _mastCamera;
        private GameObject _siteCamera;
        private GameObject _relicStandIn;
        private GameObject _earthCamera;
        private UpgradeDefinition _tower;
        private UpgradeDefinition[] _bayKit = Array.Empty<UpgradeDefinition>();

        public override void TearDown()
        {
            Object.DestroyImmediate(_friendCamera);
            Object.DestroyImmediate(_bellCamera);
            Object.DestroyImmediate(_bellStandIn);
            Object.DestroyImmediate(_mastCamera);
            Object.DestroyImmediate(_siteCamera);
            Object.DestroyImmediate(_relicStandIn);
            Object.DestroyImmediate(_earthCamera);
            Object.DestroyImmediate(_tower);
            foreach (UpgradeDefinition kit in _bayKit)
            {
                Object.DestroyImmediate(kit);
            }

            Object.DestroyImmediate(_uiHost);
            Object.DestroyImmediate(_fakesHost);
            Object.DestroyImmediate(_panel);
            Object.DestroyImmediate(_tuning);
            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
            }

            Scene scene = SceneManager.GetSceneByPath(MainScene);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            if (_slot != null)
            {
                UiTestRig.DeleteSlotFiles(_slot);
            }

            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator CaptureEveryUiState_OverTheMainScene()
        {
#if UNITY_EDITOR
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            yield return LoadMain();
            GameBootstrap bootstrap = FindBootstrap();
            GameContext context = bootstrap.Context;
            Camera camera = context.Get<IViewCamera>().Camera;

            UISystem ui = BuildUi(context, out FakeGameServices fakes, out SaveService save);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);

            context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(_tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn + 0.5f);
            yield return Capture(camera, folder, "01_title");
            Submit(ui.Layout.ResumeButton);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(_tuning.Title.Reveal.FadeOut + 0.5f);

            Transform view = camera.transform;
            Vector3 site = view.position + Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized * 9f +
                           view.right * 1.5f + Vector3.down * 1.5f;
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Excavate, site, true);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture(camera, folder, "02_prompt_dig");

            fakes.PrimaryHint = InteractionHint.None;
            fakes.TetherState = TetherAimState.Hovering;
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture(camera, folder, "03_reticle_hover");
            fakes.TetherState = TetherAimState.Idle;

            fakes.SetMaterials(4, 2, 1);
            fakes.SetMaterials(7, 2, 3);
            yield return new WaitForSecondsRealtime(_tuning.PickupFeed.MaxCountSeconds * 0.5f);
            yield return Capture(camera, folder, "04_pickup_feed_gain");
            yield return new WaitForSecondsRealtime(_tuning.PickupFeed.MaxCountSeconds);

            context.Events.Publish(new RelicDeposited("cassette_player", Vector3.zero, 3));
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.AppearDelay +
                                                    _tuning.MemoryCard.Reveal.FadeIn + 0.5f);
            yield return Capture(camera, folder, "05_memory_card");
            ui.Card.Dismiss();
            yield return new WaitForSecondsRealtime(1.5f);

            fakes.Upgrade = _tower;
            fakes.AtStation = true;
            yield return new WaitForSecondsRealtime(1.2f);
            Press(keyboard.eKey);
            yield return new WaitForSecondsRealtime(_tuning.TowerPanel.HoldSeconds * 0.55f);
            yield return Capture(camera, folder, "06_tower_holding");
            Release(keyboard.eKey);
            fakes.SetMaterials(1, 0, 3);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture(camera, folder, "07_tower_short");
            fakes.AtStation = false;
            yield return new WaitForSecondsRealtime(1f);

            Vector3 tilly = context.Get<IFriendStatuses>().Status(0).Position;
            Camera friendCamera = FriendCamera(tilly, context.Get<ITerrainQuery>(), camera);
            fakes.Camera = friendCamera;
            fakes.Position = tilly + (friendCamera.transform.position - tilly).normalized * 5f;
            fakes.TillyStatus = new FriendStatus(FriendState.Dormant, 0, 3, true, false, tilly);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture(friendCamera, folder, "08_tilly_parts_0of3");
            fakes.TillyStatus = new FriendStatus(FriendState.PartsGathering, 2, 3, true, false, tilly);
            yield return new WaitForSecondsRealtime(_tuning.Friends.PipFillSeconds * 0.5f);
            yield return Capture(friendCamera, folder, "09_tilly_parts_filling");
            yield return new WaitForSecondsRealtime(_tuning.Friends.PipFillSeconds * 2f);
            fakes.TillyStatus = new FriendStatus(FriendState.PartsGathering, 3, 3, true, true, tilly);
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Repair, tilly, true);
            yield return new WaitForSecondsRealtime(_tuning.Friends.PipFillSeconds + 1.5f);
            yield return Capture(friendCamera, folder, "10_tilly_repair_prompt");
            fakes.PrimaryHint = InteractionHint.None;
            context.Events.Publish(new FriendRepairStarted("tilly"));
            fakes.TillyStatus = new FriendStatus(FriendState.Awake, 3, 3, true, false, tilly + Vector3.up * 1.5f);
            context.Events.Publish(new FriendRepaired("tilly"));
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.AppearDelay +
                                                    _tuning.MemoryCard.Reveal.FadeIn + 0.5f);
            yield return Capture(friendCamera, folder, "11_tilly_awake_log");
            ui.Card.Dismiss();
            fakes.Camera = camera;
            yield return new WaitForSecondsRealtime(_tuning.Friends.NameHoldSeconds + 2f);

            context.Events.Publish(new TickerLine(UiTestRig.SignalLine));
            yield return new WaitForSecondsRealtime(TickerEntrance());
            yield return Capture(camera, folder, "18_ticker");
            context.Events.Publish(new CrewLogFound(UiTestRig.FirstLog, Vector3.zero));
            yield return new WaitForSecondsRealtime(CardEntrance());
            yield return Capture(camera, folder, "19_crew_log_card");
            ui.Card.Dismiss();
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.Reveal.FadeOut + 0.3f);

            fakes.AddTape(UiTestRig.FirstTape);
            context.Events.Publish(new CassetteCollected(UiTestRig.FirstTape, Vector3.zero, fakes.OwnedTapeCount,
                fakes.TotalTapeCount));
            yield return new WaitForSecondsRealtime(CardEntrance());
            yield return Capture(camera, folder, "20_liner_card");
            ui.Card.Dismiss();
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.Reveal.FadeOut + 0.3f);

            fakes.DialUnlocked = true;
            Transform corner = BellAtHome(context);
            Camera bellCamera = BellCamera(corner, camera);
            fakes.Camera = bellCamera;
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Tune, corner.position, true);
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Find(InteractionKind.Tune).DwellSeconds +
                                                    _tuning.Prompts.Reveal.FadeIn + 0.6f);
            yield return Capture(bellCamera, folder, "30_bell_home_tune_prompt");
            fakes.Tune(RadioChannel.QuietHours, UiTestRig.FirstTape);
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Reveal.FadeOut + _tuning.DialReadout.Reveal.FadeIn +
                                                    0.5f);
            yield return Capture(bellCamera, folder, "31_bell_home_dial_readout");
            fakes.PrimaryHint = InteractionHint.None;
            fakes.Camera = camera;
            yield return new WaitForSecondsRealtime(ReadoutExit());

            fakes.Tune(RadioChannel.TapeDeck, UiTestRig.FirstTape);
            yield return new WaitForSecondsRealtime(_tuning.DialReadout.Reveal.FadeIn + 0.4f);
            yield return Capture(camera, folder, "21_dial_readout");
            yield return new WaitForSecondsRealtime(ReadoutExit());

            fakes.Bay = _bayKit;
            fakes.SetMaterials(12, 7, 0);
            fakes.AtStation = true;
            yield return new WaitForSecondsRealtime(1.8f);
            yield return Tap(keyboard.eKey);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Capture(camera, folder, "22_bay_choosing");
            yield return Tap(keyboard.eKey);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Capture(camera, folder, "22b_bay_short_pick");
            fakes.AtStation = false;
            fakes.Bay = null;
            yield return new WaitForSecondsRealtime(1f);

            if (!context.Get<IWorldAnchors>().TryGet(DepotSite, out MoonProject.Core.WorldAnchor depot))
            {
                throw new InvalidOperationException($"The world has no '{DepotSite}' anchor.");
            }

            Camera siteCamera = SiteCamera(depot, camera);
            Vector3 cut = depot.Position + Vector3.up * CutHeight;
            fakes.Camera = siteCamera;
            fakes.HasTarget = true;
            fakes.CutPoint = cut;
            fakes.Material = SalvageMaterial.Wiring;
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Salvage, cut, true);
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Find(InteractionKind.Salvage).DwellSeconds +
                                                    _tuning.Prompts.Reveal.FadeIn + 0.8f);
            yield return Capture(siteCamera, folder, "40_salvage_cut_prompt");
            fakes.IsCutting = true;
            fakes.CutProgress = CutHeld * 0.5f;
            yield return new WaitForSecondsRealtime(0.5f);
            fakes.IsCutting = false;
            fakes.CutProgress = CutHeld;
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Reveal.FadeOut + _tuning.Salvage.Ring.FadeIn +
                                                    0.5f);
            yield return Capture(siteCamera, folder, "41_salvage_ring_kept");
            fakes.HasTarget = false;
            fakes.CutProgress = 0f;
            fakes.PrimaryHint = InteractionHint.None;
            context.Events.Publish(new SiteAnswered(DepotSite, depot.Position, 30f, true));
            Vector3 namePoint = depot.Position + Vector3.up * _tuning.Salvage.SiteNameLiftMetres;
            siteCamera.transform.rotation = Quaternion.LookRotation(
                Vector3.Lerp(cut, namePoint, 0.5f) - siteCamera.transform.position, Vector3.up);
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Reveal.FadeOut);
            yield return new WaitForSecondsRealtime(_tuning.Salvage.SiteName.FadeIn + 0.5f);
            yield return Capture(siteCamera, folder, "42_site_name");
            Assert.IsTrue(ui.SiteName.IsVisible, "the site's name was up for its shot");
            fakes.Camera = camera;
            yield return new WaitForSecondsRealtime(_tuning.Salvage.SiteNameHoldSeconds +
                                                    _tuning.Salvage.SiteName.FadeOut + 0.3f);

            Vector3 relic = SurfacedRelic(context.Get<ITerrainQuery>(), camera);
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Stow, relic, true);
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Find(InteractionKind.Stow).DwellSeconds +
                                                    _tuning.Prompts.Reveal.FadeIn + 0.8f);
            yield return Capture(camera, folder, "43_stow_prompt");
            fakes.PrimaryHint = InteractionHint.None;
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Reveal.FadeOut + 0.3f);
            Object.DestroyImmediate(_relicStandIn);

            fakes.Bay = _bayKit;
            fakes.SetMaterials(12, 7, 2);
            fakes.AtStation = true;
            yield return new WaitForSecondsRealtime(1.8f);
            yield return Tap(keyboard.eKey);
            yield return new WaitForSecondsRealtime(0.4f);
            Press(keyboard.eKey);
            yield return new WaitForSecondsRealtime(_tuning.TowerPanel.TapSeconds + _tuning.TowerPanel.HoldSeconds +
                                                    0.3f);
            Release(keyboard.eKey);
            yield return new WaitForSecondsRealtime(KitSettleSeconds);
            yield return Capture(camera, folder, "44_bay_working");
            context.Events.Publish(new PartFitted(RoverSocket.Hold, RoverKitPiece.CargoRack, false, false, default, 0));
            yield return new WaitForSecondsRealtime(LineShotSeconds);
            yield return Capture(camera, folder, "44b_bay_fitted_line");
            yield return new WaitForSecondsRealtime(_tuning.KitTitle.Delay + _tuning.KitTitle.Reveal.FadeIn);
            yield return Capture(camera, folder, "44c_kit_title_crafted");
            fakes.AtStation = false;
            yield return new WaitForSecondsRealtime(_tuning.KitTitle.HoldSeconds + _tuning.KitTitle.Reveal.FadeOut +
                                                    0.3f);
            yield return new WaitForSecondsRealtime(KitSettleSeconds);
            context.Events.Publish(new PartFitted(RoverSocket.Wing, RoverKitPiece.SolarCell, true, false, default, 0));
            yield return new WaitForSecondsRealtime(_tuning.KitTitle.Delay + _tuning.KitTitle.Reveal.FadeIn + 0.4f);
            yield return Capture(camera, folder, "45_kit_title_gift");
            fakes.Bay = null;
            yield return new WaitForSecondsRealtime(_tuning.KitTitle.HoldSeconds + _tuning.KitTitle.Reveal.FadeOut +
                                                    0.3f);

            fakes.SetMaterials(12, 7, 2);
            fakes.AtStation = true;
            yield return new WaitForSecondsRealtime(1.5f);
            Press(keyboard.eKey);
            yield return new WaitForSecondsRealtime(_tuning.TowerPanel.HoldSeconds + 0.3f);
            Release(keyboard.eKey);
            yield return new WaitForSecondsRealtime(TowerStitchDelay);
            context.Events.Publish(new StationCued(StationCue.StitchStarted, _tower.Id,
                context.Get<IRoverState>().Position));
            yield return new WaitForSecondsRealtime(LineShotSeconds);
            yield return Capture(camera, folder, "47_tower_stitch_line");
            fakes.AtStation = false;
            yield return new WaitForSecondsRealtime(_tuning.TowerPanel.CelebrateSeconds + 1f);

            Transform socket = DarkMastSocket(context.Get<IWorldAnchors>());
            Camera mastCamera = MastCamera(socket, camera);
            fakes.Camera = mastCamera;
            fakes.NextCost = new Recipe(6, 4, 0);
            fakes.SetMaterials(2, 4, 0);
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Restore, socket.position, false);
            yield return new WaitForSecondsRealtime(_tuning.Relays.Tag.FadeIn + 1f);
            yield return Capture(mastCamera, folder, "32_relay_tag_short");
            fakes.SetMaterials(8, 5, 0);
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Restore, socket.position, true);
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Find(InteractionKind.Restore).DwellSeconds +
                                                    _tuning.Prompts.Reveal.FadeIn + 0.8f);
            fakes.RestoreHold = HopHold;
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Capture(mastCamera, folder, "33_relay_restore_prompt");
            fakes.RestoreHold = 0f;
            fakes.PrimaryHint = InteractionHint.None;
            fakes.Camera = camera;
            yield return new WaitForSecondsRealtime(1f);

            fakes.SetHopChoices(UiTestRig.FirstRelayNode, UiTestRig.SecondRelayNode);
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Hop, context.Get<IRoverState>().Position, true);
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Find(InteractionKind.Hop).DwellSeconds +
                                                    _tuning.Prompts.Reveal.FadeIn + 0.8f);
            yield return Capture(camera, folder, "34_hop_prompt");
            fakes.Open();
            fakes.ConfirmHold = HopHold;
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Reveal.FadeOut + _tuning.Relays.List.FadeIn + 0.5f);
            yield return Capture(camera, folder, "35_hop_list");
            fakes.ConfirmHold = 0f;
            fakes.Confirm();
            fakes.PrimaryHint = InteractionHint.None;
            fakes.Fade = HopMidFade;
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture(camera, folder, "36_hop_mid_fade");
            fakes.Fade = 1f;
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture(camera, folder, "37_hop_dark");
            fakes.Phase = RadioHopPhase.Arriving;
            fakes.Fade = 0f;
            yield return new WaitForSecondsRealtime(1f);
            fakes.Phase = RadioHopPhase.Closed;

            fakes.LitMasts = 2;
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture(camera, folder, "38_pause_relays");
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1f);

            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture(camera, folder, "12_pause");
            Assert.AreEqual(1f, ui.Backdrop.Weight, 0.01f, "the frame behind the pause menu is blurred");
            Submit(ui.Layout.SettingsButton);
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture(camera, folder, "13_pause_settings");
            Submit(ui.Layout.LanguageButton);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture(camera, folder, "14_pause_settings_vi");
            yield return Tap(keyboard.escapeKey);
            Submit(ui.Layout.QuitButton);
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture(camera, folder, "15_pause_quit_vi");
            yield return Tap(keyboard.escapeKey);
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1f);

            context.Events.Publish(new RelicDeposited("rubber_duck", Vector3.zero, 4));
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.AppearDelay +
                                                    _tuning.MemoryCard.Reveal.FadeIn + 0.5f);
            yield return Capture(camera, folder, "16_memory_card_vi");
            ui.Card.Dismiss();
            yield return new WaitForSecondsRealtime(1.5f);
            context.Events.Publish(new FriendRepaired("tilly"));
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.AppearDelay +
                                                    _tuning.MemoryCard.Reveal.FadeIn + 0.5f);
            yield return Capture(camera, folder, "17_tilly_log_vi");
            ui.Card.Dismiss();
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.Reveal.FadeOut + 0.3f);

            context.Events.Publish(new TickerLine(UiTestRig.HomeLine));
            yield return new WaitForSecondsRealtime(TickerEntrance());
            yield return Capture(camera, folder, "23_ticker_vi");
            fakes.AddTape(UiTestRig.SecondTape);
            context.Events.Publish(new CassetteCollected(UiTestRig.SecondTape, Vector3.zero, fakes.OwnedTapeCount,
                fakes.TotalTapeCount));
            yield return new WaitForSecondsRealtime(CardEntrance());
            yield return Capture(camera, folder, "24_liner_card_vi");
            ui.Card.Dismiss();
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.Reveal.FadeOut + 0.3f);
            fakes.Tune(RadioChannel.TapeDeck, UiTestRig.SecondTape);
            yield return new WaitForSecondsRealtime(_tuning.DialReadout.Reveal.FadeIn + 0.4f);
            yield return Capture(camera, folder, "25_dial_readout_vi");
            yield return new WaitForSecondsRealtime(ReadoutExit());
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture(camera, folder, "26_pause_cassettes_vi");
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1f);

            Reframe(NarrowWidth, NarrowHeight);
            context.Events.Publish(new TickerLine(UiTestRig.SignalLine));
            fakes.AtStation = true;
            yield return new WaitForSecondsRealtime(TickerEntrance() + 0.5f);
            yield return Capture(camera, folder, "27_narrow_ticker_and_tower_vi");
            fakes.AtStation = false;
            fakes.Tune(RadioChannel.QuietHours, UiTestRig.SecondTape);
            yield return new WaitForSecondsRealtime(_tuning.DialReadout.Reveal.FadeIn + 0.4f);
            yield return Capture(camera, folder, "28_narrow_dial_readout_vi");
            yield return new WaitForSecondsRealtime(ReadoutExit());
            context.Events.Publish(new CrewLogFound(UiTestRig.FirstLog, Vector3.zero));
            yield return new WaitForSecondsRealtime(CardEntrance());
            yield return Capture(camera, folder, "29_narrow_crew_log_card_vi");
            Assert.IsTrue(save.SaveNow(), "the UI saved to its test slot");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// The floating names at their hardest: Tilly's as she wakes, Bell's in her home corner (her canyon hides
        /// Earth), and a site's as it answers, each
        /// seen from below with the bright Earth right behind it. Quick to rerun on its own:
        /// <c>python tools/unity_batch.py tests --platform playmode --filter CaptureFloatingNames_OverEarth</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureFloatingNames_OverEarth()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            UISystem ui = BuildUi(context, out FakeGameServices fakes, out _);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            Vector3 earth = context.Get<IWorldLayout>().EarthDirection;
            var terrain = context.Get<ITerrainQuery>();
            var friends = context.Get<IFriendStatuses>();
            Vector3 tilly = FriendPosition(friends, TillyId) + Vector3.up * TillyRise;
            Camera tillyCamera = EarthCamera(tilly + Vector3.up * _tuning.Friends.NameLiftFor(TillyId), earth, terrain, context);
            fakes.Camera = tillyCamera;
            fakes.TillyStatus = new FriendStatus(FriendState.Awake, 3, 3, true, false, tilly);
            context.Events.Publish(new FriendRepaired(TillyId));
            yield return new WaitForSecondsRealtime(_tuning.Friends.Name.FadeIn + 0.5f);
            yield return Capture(tillyCamera, folder, "50_tilly_name_over_earth");
            ui.Card.Dismiss();
            yield return new WaitForSecondsRealtime(_tuning.Friends.NameHoldSeconds + _tuning.Friends.Name.FadeOut +
                                                    0.3f);

            Vector3 bell = BellAtHome(context).position;
            Camera bellCamera = EarthCamera(bell + Vector3.up * _tuning.Friends.NameLiftFor(BellId), earth, terrain, context);
            fakes.Camera = bellCamera;
            fakes.Friend = AssetDatabase.LoadAssetAtPath<FriendDefinition>(BellPath);
            fakes.TillyStatus = new FriendStatus(FriendState.Awake, 3, 3, true, false, bell);
            context.Events.Publish(new FriendRepaired(BellId));
            yield return new WaitForSecondsRealtime(_tuning.Friends.Name.FadeIn + 0.5f);
            yield return Capture(bellCamera, folder, "51_bell_name_over_earth");
            ui.Card.Dismiss();
            yield return new WaitForSecondsRealtime(_tuning.Friends.NameHoldSeconds + _tuning.Friends.Name.FadeOut +
                                                    0.3f);

            if (!context.Get<IWorldAnchors>().TryGet(DepotSite, out MoonProject.Core.WorldAnchor depot))
            {
                throw new InvalidOperationException($"The world has no '{DepotSite}' anchor.");
            }

            Camera siteCamera = EarthCamera(depot.Position + Vector3.up * _tuning.Salvage.SiteNameLiftMetres, earth,
                terrain, context);
            fakes.Camera = siteCamera;
            context.Events.Publish(new SiteAnswered(DepotSite, depot.Position, 30f, true));
            yield return new WaitForSecondsRealtime(_tuning.Salvage.SiteName.FadeIn + 0.5f);
            yield return Capture(siteCamera, folder, "52_site_name_over_earth");
            Assert.IsTrue(ui.SiteName.IsVisible, "the site's name was up for its shot");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// The title's New Game question over a save, resting on Keep this moon. Rerun on its own:
        /// <c>python tools/unity_batch.py tests --platform playmode --filter CaptureNewGameQuestion</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureNewGameQuestion()
        {
#if UNITY_EDITOR
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out _, out _);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(_tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn);
            Submit(ui.Layout.NewGameButton);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(ui.Pause.IsAskingNewGame);
            yield return Capture(camera, folder, "39_title_new_game");
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(ui.Pause.IsTitle && !ui.Pause.IsAskingNewGame, "Esc keeps this moon, back to the menu");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// A place's card the first time 07 comes near, then the soft next line once 07 rests, the opening's protocol
        /// readout and a recipe card while looking at the tower's hopper (M3-24 B). Rerun on its own:
        /// <c>python tools/unity_batch.py tests --platform playmode --filter CaptureGuidance</c>.
        /// </summary>
        /// <summary>
        /// The feel pass's UI (docs/design/feel-pass.md): the stock count rolling up a unit at a time with a tick event
        /// per unit (a second collect carries the roll on), and the hold ring's charge arc filling on a cut's
        /// Anticipate beat, then handing over to the progress fill at ignition. Captures 70-73. Rerun with
        /// --filter CaptureFeelPass.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureFeelPass_TheRollAndTheChargingRing()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out FakeGameServices fakes, out _);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(0.5f);

            var ticks = new List<StockCountTicked>();
            using (context.Events.Subscribe<StockCountTicked>(ticks.Add))
            {
                fakes.SetMaterials(2, 0, 0);
                yield return new WaitForSecondsRealtime(_tuning.PickupFeed.LingerSeconds * 0.3f);
                Assert.AreEqual(2, ui.Pickups.Shown(SalvageMaterial.Metal));
                ticks.Clear();

                fakes.SetMaterials(8, 0, 0);
                yield return new WaitForSecondsRealtime(_tuning.PickupFeed.StepSeconds * 2.5f);
                int midway = ui.Pickups.Shown(SalvageMaterial.Metal);
                Assert.That(midway, Is.InRange(3, 7), "the count is rolling, not jumping");
                yield return Capture(camera, folder, "70_stock_rolling");
                fakes.SetMaterials(10, 0, 0);
                yield return new WaitForSecondsRealtime(_tuning.PickupFeed.StepSeconds * 12f);
                Assert.AreEqual(10, ui.Pickups.Shown(SalvageMaterial.Metal));
                yield return Capture(camera, folder, "71_stock_rolled");

                // The scene's own UI answers the same stock event, so each tick arrives once per UI: count units.
                var units = new SortedDictionary<int, int>();
                foreach (StockCountTicked tick in ticks)
                {
                    units[tick.Index] = tick.Remaining;
                }

                Assert.AreEqual(8, units.Count, "a tick for each of the eight units from 2 to 10");
                int expected = 0;
                foreach (KeyValuePair<int, int> unit in units)
                {
                    Assert.AreEqual(expected, unit.Key, "the index keeps climbing through a second collect");
                    expected++;
                }

                Assert.AreEqual(0, units[7], "the last unit has none to come: the roll lands on 10");
            }

            if (!context.Get<IWorldAnchors>().TryGet(DepotSite, out MoonProject.Core.WorldAnchor depot))
            {
                throw new InvalidOperationException($"The world has no '{DepotSite}' anchor.");
            }

            Camera siteCamera = SiteCamera(depot, camera);
            Vector3 cut = depot.Position + Vector3.up * CutHeight;
            fakes.Camera = siteCamera;
            fakes.HasTarget = true;
            fakes.CutPoint = cut;
            fakes.Material = SalvageMaterial.Wiring;
            for (int warm = 0; warm < 5; warm++)
            {
                yield return null;
            }

            context.Events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Anticipate, cut, Vector3.up, 0f, 0.65f,
                SalvageMaterial.Wiring));
            float last = 0f;
            bool seenFilling = false;
            float end = Time.realtimeSinceStartup + 2f;
            while (ui.SalvageRing.Charge < 1f && Time.realtimeSinceStartup < end)
            {
                float now = ui.SalvageRing.Charge;
                Assert.GreaterOrEqual(now, last, "the charge only fills");
                last = now;
                if (!seenFilling && now > 0.3f)
                {
                    seenFilling = true;
                    Assert.IsTrue(ui.SalvageRing.IsVisible, "the ring is up for the charge, before any progress");
                    yield return Capture(siteCamera, folder, "72_ring_charging");
                }

                yield return null;
            }

            Assert.IsTrue(seenFilling, "the charge segment was seen filling");
            Assert.AreEqual(1f, ui.SalvageRing.Charge, 1e-3f, "full by the end of the beat");
            fakes.IsCutting = true;
            fakes.CutProgress = 0.3f;
            context.Events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Act, cut, Vector3.up, 0f, 0f,
                SalvageMaterial.Wiring));
            yield return new WaitForSecondsRealtime(_tuning.Salvage.ChargeHandoffSeconds * 0.4f);
            Assert.AreEqual(0.3f, ui.SalvageRing.Progress, 0.02f, "the progress fill follows from ignition");
            yield return Capture(siteCamera, folder, "73_ring_ignited");
            yield return new WaitForSecondsRealtime(_tuning.Salvage.ChargeHandoffSeconds);
            Assert.AreEqual(0f, ui.SalvageRing.Charge, "the charge has handed over");

            fakes.IsCutting = false;
            context.Events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Cancel, cut, Vector3.up, 0.3f, 0f,
                SalvageMaterial.Wiring));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(0.3f, ui.SalvageRing.Progress, 0.02f, "releasing keeps the progress, still on the ring");
            Assert.IsTrue(ui.SalvageRing.IsVisible);
            fakes.HasTarget = false;
            fakes.CutProgress = 0f;
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// Polish wave 1: the save tick after a GameSaved, and a sleeping station's not-yet line on the ticker. Captures
        /// 80 and 81. Rerun with --filter CapturePolishWave1.
        /// </summary>
        [UnityTest]
        public IEnumerator CapturePolishWave1_TheSaveTickAndTheNotYetLine()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out _, out _);
            FreezeWorld();
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(0.5f);

            context.Events.Publish(new GameSaved());
            yield return new WaitForSecondsRealtime(_tuning.SaveTick.Reveal.FadeIn + 0.2f);
            Assert.IsTrue(ui.SaveTick.IsVisible, "the tick is up after a save");
            yield return Capture(camera, folder, "80_save_tick");
            yield return new WaitForSecondsRealtime(_tuning.SaveTick.HoldSeconds + _tuning.SaveTick.Reveal.FadeOut + 0.5f);
            Assert.IsFalse(ui.SaveTick.IsVisible, "and gone a moment later");

            context.Events.Publish(new StationNotYet("notyet.bay", Vector3.zero, 10f));
            yield return Until(() => ui.Ticker.IsVisible, WaitLimitSeconds);
            yield return new WaitForSecondsRealtime(_tuning.Ticker.Reveal.FadeIn + 0.3f);
            Assert.IsTrue(ui.Ticker.IsVisible, "the sleeper's line is on the ticker");
            yield return Capture(camera, folder, "81_not_yet_ticker");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>The settings' three tabs over the title (captures 92-94). Rerun with --filter CaptureSettingsTabs.</summary>
        [UnityTest]
        public IEnumerator CaptureSettingsTabs_SoundCameraDisplay()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out _, out _);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(_tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn + 0.5f);
            Submit(ui.Layout.SettingsButton);
            yield return new WaitForSecondsRealtime(_tuning.Pause.SettingsPanel.FadeIn + 0.5f);
            yield return Capture(camera, folder, "92_settings_sound");
            Submit(ui.Layout.TabCamera);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Capture(camera, folder, "93_settings_camera");
            Submit(ui.Layout.TabDisplay);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Capture(camera, folder, "94_settings_display");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// The cargo screen's two pages, the unlock card and a lunar pickup row (captures 95-98). Rerun with
        /// --filter CaptureCargoScreen.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureCargoScreen_HoldHomeSevenCan_UnlockCard_AndALunarPickup()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out FakeGameServices fakes, out _);
            FreezeWorld();
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(0.5f);
            fakes.Hold.Set(3, new MaterialSlot(SalvageMaterial.Metal, 4, 4), new MaterialSlot(SalvageMaterial.Metal, 2, 4),
                new MaterialSlot(SalvageMaterial.Wiring, 3, 4), new MaterialSlot(SalvageMaterial.Optics, 1, 4),
                new MaterialSlot(SalvageMaterial.Ilmenite, 5, 6));
            fakes.Hold.WaitingBundles = 3;
            fakes.Grant(RoverAbility.Tether);
            fakes.Grant(RoverAbility.WideSonar);
            fakes.SetTech(TechNodeId.HoverJump, TechNodeState.Decoded);
            ui.Cargo.Open();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeIn + _tuning.Cargo.BootTypeSeconds +
                                                    _tuning.Cargo.BootLightStepSeconds * 3f);
            Assert.IsTrue(ui.Cargo.IsBooting, "the first press boots the hold");
            yield return Capture(camera, folder, "94_hold_boot_lights");
            yield return new WaitForSecondsRealtime(2f);
            Assert.IsTrue(ui.Cargo.IsOpen && !ui.Cargo.IsBooting);
            yield return Capture(camera, folder, "95_cargo_screen");
            ui.Cargo.Close();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeOut + 0.4f);
            fakes.Home.Set(3, new MaterialSlot(SalvageMaterial.Metal, 4, 4), new MaterialSlot(SalvageMaterial.Wiring, 4, 4),
                new MaterialSlot(SalvageMaterial.Optics, 2, 4), new MaterialSlot(SalvageMaterial.Ilmenite, 6, 6));
            fakes.Dock.IsDocked = true;
            ui.Cargo.Open();
            ui.Cargo.FocusFirst();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeIn + 0.6f);
            Assert.IsTrue(ui.Cargo.IsTransferView, "docked, the hold sits beside home's shelves");
            yield return Capture(camera, folder, "95b_transfer_view");
            fakes.Dock.IsDocked = false;
            ui.Cargo.Close();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeOut + 0.4f);
            fakes.Equipment.Fit(RoverKitPiece.TetherRing);
            fakes.Equipment.Fit(RoverKitPiece.SolarCell);
            fakes.Equipment.Craft(RoverKitPiece.HoverCoils);
            fakes.Equipment.Craft(RoverKitPiece.FocusLens);
            fakes.Equipment.AtStation = false;
            ui.Cargo.SelectTab(1);
            ui.Cargo.Open();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeIn + 0.6f);
            yield return Capture(camera, folder, "96_seven_tab_away");
            fakes.Equipment.AtStation = true;
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Capture(camera, folder, "96b_seven_tab_at_the_bay");
            ui.Cargo.SelectTab(0);
            ui.Cargo.Close();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeOut + 0.6f);

            ui.UnlockCard.Gained(RoverAbility.Tether);
            yield return new WaitForSecondsRealtime(_tuning.UnlockCard.Reveal.FadeIn + 0.8f);
            Assert.IsTrue(ui.UnlockCard.IsVisible);
            yield return Capture(camera, folder, "97_unlock_card");
            yield return new WaitForSecondsRealtime(_tuning.UnlockCard.HoldSeconds + _tuning.UnlockCard.Reveal.FadeOut);

            fakes.SetUnits(SalvageMaterial.Ilmenite, 8);
            fakes.SetUnits(SalvageMaterial.WaterIce, 7);
            yield return new WaitForSecondsRealtime(_tuning.PickupFeed.MaxCountSeconds * 0.5f + 0.4f);
            yield return Capture(camera, folder, "98_pickup_lunar");

            Transform view = camera.transform;
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Build,
                view.position + Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized * 6f + Vector3.down * 1.2f,
                true);
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Reveal.FadeIn + 1.5f);
            yield return Capture(camera, folder, "99_build_prompt");
            fakes.PrimaryHint = InteractionHint.None;
            context.Events.Publish(new CrewCrateOpened("site.depot", "kenji", SalvageMaterial.Metal, 3, Vector3.zero));
            yield return new WaitForSecondsRealtime(_tuning.CrateNote.Reveal.FadeIn + 1f);
            yield return Capture(camera, folder, "100_crate_note");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>The credits over the title (capture 90). Rerun with --filter CaptureCredits.</summary>
        [UnityTest]
        public IEnumerator CaptureCredits_OverTheTitle()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out _, out _);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(_tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn + 0.5f);
            Submit(ui.Layout.CreditsButton);
            yield return new WaitForSecondsRealtime(_tuning.Credits.Reveal.FadeIn + 0.5f);
            Assert.IsTrue(ui.Pause.IsShowingCredits);
            yield return Capture(camera, folder, "90_credits_top");
            ui.Layout.CreditsScroll.scrollOffset = new Vector2(0f, 700f);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Capture(camera, folder, "91_credits_fonts_and_voice");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator CaptureGuidance()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out FakeGameServices fakes, out _);
            TuneQuickPatience();
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(0.5f);

            context.Events.Publish(new NextStepChanged("next.tower.pad"));
            context.Events.Publish(new PlaceFirstApproached("place.tower"));
            yield return new WaitForSecondsRealtime(_tuning.Guidance.TowerCardDelay + _tuning.Guidance.Place.FadeIn +
                                                    0.5f);
            yield return Capture(camera, folder, "48_place_card_tower");
            yield return new WaitForSecondsRealtime(_tuning.Guidance.PlaceHoldSeconds +
                                                    _tuning.Guidance.Place.FadeOut + _tuning.Guidance.Line.FadeIn + 0.5f);
            yield return Capture(camera, folder, "49_next_line");

            context.Events.Publish(new RoverWakeBeatReached(WakeBeat.Crackle, false));
            context.Events.Publish(new RoverWakeBeatReached(WakeBeat.GazeEarth, false));
            yield return new WaitForSecondsRealtime(_tuning.Guidance.Protocol.FadeIn + 0.5f);
            Assert.IsTrue(ui.Protocol.IsVisible, "the protocol readout shows at Earth's gaze (M3-24 B)");
            yield return Capture(camera, folder, "49b_protocol_readout");
            yield return new WaitForSecondsRealtime(_tuning.Guidance.ProtocolHoldSeconds +
                                                    _tuning.Guidance.Protocol.FadeOut + 0.5f);
            Assert.IsFalse(ui.Protocol.IsVisible, "and fades by itself");

            fakes.Upgrade = _tower;
            fakes.AtStation = false;
            fakes.AwaitingPart = true;
            fakes.SetMaterials(1, 0, 0);
            fakes.Looked = _tower.Id;
            yield return new WaitForSecondsRealtime(_tuning.Guidance.Recipe.FadeIn + 0.5f);
            Assert.AreEqual(_tower.Id, ui.RecipeCard.Shown, "looking at the tower's hopper shows its recipe card");
            yield return Capture(camera, folder, "49c_recipe_card");
            fakes.Looked = null;
            yield return new WaitForSecondsRealtime(_tuning.Guidance.Recipe.FadeOut + 0.5f);
            Assert.IsFalse(ui.RecipeCard.IsVisible, "looking away hides it");

            fakes.Bay = _bayKit;
            fakes.IsSheet = true;
            fakes.Looked = _bayKit[0].Id;
            yield return new WaitForSecondsRealtime(_tuning.Guidance.Recipe.FadeIn + 0.5f);
            Assert.AreEqual(_bayKit[0].Id, ui.RecipeCard.Shown, "a sheet on Kenji's board shows its kit's card");
            yield return Capture(camera, folder, "49d_recipe_card_board");
            fakes.Looked = null;
            fakes.IsSheet = false;
            fakes.Bay = null;

            // A prompt whose point sits under the recipe card rests on top of it, never over its words.
            fakes.Looked = _tower.Id;
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Deposit,
                camera.ViewportToWorldPoint(new Vector3(0.8f, 0.55f, 10f)), true);
            yield return new WaitForSecondsRealtime(_tuning.Guidance.Recipe.FadeIn + 2.5f);
            Assert.IsFalse(ui.Prompt.IsVisible && ui.RecipeCard.IsVisible, "one guidance element at a time");
            yield return Capture(camera, folder, "49e_recipe_card_with_prompt");
            fakes.PrimaryHint = InteractionHint.None;
            fakes.Looked = null;
            yield return new WaitForSecondsRealtime(_tuning.Guidance.Recipe.FadeOut + 0.5f);

            // The tower's reward card, from the upgrade's own data.
            context.Events.Publish(new UpgradePurchased(_tower.Id, 1));
            context.Events.Publish(new FeelBeat(FeelKind.Build, FeelPhase.Reward, Vector3.zero, Vector3.up, 1f, 0f));
            yield return new WaitForSecondsRealtime(_tuning.RewardCard.Reveal.FadeIn + 1f);
            Assert.IsTrue(ui.RewardCard.IsVisible);
            Assert.Greater(ui.RewardCard.LineCount, 0, "the card says what the level gave");
            yield return Capture(camera, folder, "49f_reward_card");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// Quality check of the first ten minutes (M3-16 "one voice at a time"): the opening's cards, lines and prompts
        /// replayed in order over the Main scene, every frame checked so that no two voices (title, place or blueprint
        /// card, story card, kit title, ticker, prompt, site name, Look up) share the screen, and each moment
        /// captured as 6x_*. Ends on Tilly's repair: her story card waits for the repair prompt's fade. The quiet next
        /// line may stay under them. Rerun with --filter CaptureFirstMinutes.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureFirstMinutes_OneVoiceAtATime()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out FakeGameServices fakes, out _);
            FreezeWorld();
            TuneQuickPatience();
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            var overlaps = new List<string>();
            EventBus events = context.Events;
            fakes.SetMaterials(0, 0, 0);

            events.Publish(new TitleShown(true));
            yield return Watch(ui, overlaps, _tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn + 0.3f);
            yield return Capture(camera, folder, "60_title");
            Submit(ui.Layout.ResumeButton);
            events.Publish(new RoverAwoke(Vector3.zero, false));
            events.Publish(new NextStepChanged("next.wake"));
            events.Publish(new PlaceFirstApproached("place.dock"));
            yield return Watch(ui, overlaps, _tuning.Title.Reveal.FadeOut + _tuning.Guidance.Place.FadeIn + 1.6f);
            yield return Capture(camera, folder, "61_dock_card");

            yield return Watch(ui, overlaps, _tuning.Guidance.PlaceHoldSeconds + _tuning.Guidance.Place.FadeOut);
            events.Publish(new SonarPinged(fakes.Position, 140f));
            events.Publish(new TickerLine("ticker.listen.first"));
            yield return Watch(ui, overlaps, 1.5f);
            yield return Capture(camera, folder, "62_first_listen");
            if (!context.Get<IWorldAnchors>().TryGet(DepotSite, out MoonProject.Core.WorldAnchor depot))
            {
                throw new InvalidOperationException($"The world has no '{DepotSite}' anchor.");
            }

            events.Publish(new SiteAnswered(DepotSite, depot.Position, 30f, true));
            yield return Watch(ui, overlaps, _tuning.Salvage.SiteName.FadeIn + 0.5f);
            yield return Capture(camera, folder, "63_site_answers");

            yield return Watch(ui, overlaps, _tuning.Salvage.SiteNameHoldSeconds + _tuning.Salvage.SiteName.FadeOut);
            fakes.SetMaterials(3, 1, 0);
            events.Publish(new NextStepChanged("next.tower.pad"));
            events.Publish(new PlaceFirstApproached("place.tower"));
            yield return Watch(ui, overlaps, _tuning.Guidance.Place.FadeIn + 0.5f);
            yield return Capture(camera, folder, "64_tower_card");
            yield return Watch(ui, overlaps, _tuning.Guidance.PlaceHoldSeconds + _tuning.Guidance.Place.FadeOut +
                                             0.5f);
            events.Publish(new FragmentScanned("fragment.cradle.1", "rover.cargo_cradle", 1, 3, false));
            yield return Watch(ui, overlaps, _tuning.CrateNote.Reveal.FadeIn + 0.8f);
            Assert.IsTrue(ui.CrateNote.IsVisible, "the scan's pickup note is up");
            StringAssert.Contains("2", ui.CrateNote.Text);
            yield return Capture(camera, folder, "65_scan_note");
            events.Publish(new BlueprintCompleted("rover.cargo_cradle"));
            yield return Watch(ui, overlaps, _tuning.CrateNote.HoldSeconds(40) + _tuning.CrateNote.Reveal.FadeOut + 0.5f);
            yield return Watch(ui, overlaps, _tuning.Guidance.PlaceHoldSeconds + _tuning.Guidance.Place.FadeOut +
                                             TickerEntrance());
            yield return Capture(camera, folder, "66_plotter_line");
            yield return Watch(ui, overlaps, _tuning.Ticker.Reveal.FadeOut + 12f);
            yield return Capture(camera, folder, "67_next_line");

            // A card with no delay of its own must still wait for the repair prompt to finish fading.
            var serialized = new SerializedObject(_tuning);
            serialized.FindProperty("_memoryCard._appearDelay").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Vector3 tilly = context.Get<IFriendStatuses>().Status(0).Position;
            Camera friendCamera = FriendCamera(tilly, context.Get<ITerrainQuery>(), camera);
            fakes.Camera = friendCamera;
            fakes.Position = tilly + (friendCamera.transform.position - tilly).normalized * 5f;
            fakes.TillyStatus = new FriendStatus(FriendState.PartsGathering, 3, 3, true, true, tilly);
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Repair, tilly, true);
            yield return Watch(ui, overlaps, _tuning.Guidance.Line.FadeOut + _tuning.Prompts.Find(InteractionKind.Repair).DwellSeconds +
                                             _tuning.Prompts.Reveal.FadeIn + 0.5f);
            Assert.IsTrue(ui.Prompt.IsVisible, "the repair prompt was up before the repair");
            yield return Capture(friendCamera, folder, "68_repair_prompt");
            fakes.PrimaryHint = InteractionHint.None;
            events.Publish(new FriendRepairStarted(TillyId));
            fakes.TillyStatus = new FriendStatus(FriendState.Awake, 3, 3, true, false, tilly + Vector3.up * TillyRise);
            events.Publish(new FriendRepaired(TillyId));
            yield return Watch(ui, overlaps, _tuning.Prompts.Reveal.FadeOut + _tuning.MemoryCard.Reveal.FadeIn + 0.5f);
            yield return Capture(friendCamera, folder, "69_tilly_card");
            Assert.IsTrue(ui.Card.IsVisible, "Tilly's card came once the prompt had gone");
            Assert.IsEmpty(overlaps, "one voice at a time:\n" + string.Join("\n", overlaps));
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

#if UNITY_EDITOR
        /// <summary>The step patience shortened so a capture sees the next line without two minutes' wait.</summary>
        private void TuneQuickPatience()
        {
            var serialized = new SerializedObject(_tuning);
            serialized.FindProperty("_guidance._stepPatienceSeconds").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Holds the live world still: its own sonar, home and rover would answer over the scripted moments, and the
        /// UI runs on unscaled time. Restored in <see cref="TearDown"/>.
        /// </summary>
        private static void FreezeWorld()
        {
            Time.timeScale = 0f;
        }

        /// <summary>Waits until <paramref name="condition"/> holds, for at most <paramref name="seconds"/>.</summary>
        private static IEnumerator Until(Func<bool> condition, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end && !condition())
            {
                yield return null;
            }
        }

        /// <summary>Waits <paramref name="seconds"/>, noting every frame where more than one voice speaks.</summary>
        private static IEnumerator Watch(UISystem ui, List<string> overlaps, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                string voices = Voices(ui);
                if (voices.IndexOf('+') >= 0 && !overlaps.Contains(voices))
                {
                    overlaps.Add(voices);
                }

                yield return null;
            }
        }

        private static string Voices(UISystem ui)
        {
            var on = new List<string>();
            if (ui.Pause.IsTitle)
            {
                on.Add("title");
            }

            if (ui.PlaceCard.IsVisible)
            {
                on.Add("card " + ui.PlaceCard.Current);
            }

            if (ui.Card.IsVisible)
            {
                on.Add("story card");
            }

            if (ui.KitTitle.IsVisible)
            {
                on.Add("kit title");
            }

            if (ui.Ticker.IsVisible)
            {
                on.Add("ticker");
            }

            if (ui.Prompt.IsVisible)
            {
                on.Add("prompt");
            }

            if (ui.SiteName.IsVisible)
            {
                on.Add("site name");
            }

            if (ui.LookUp.IsVisible)
            {
                on.Add("look up");
            }

            return string.Join(" + ", on);
        }

        private static IEnumerator LoadMain()
        {
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(MainScene,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(SceneSettleSeconds);
        }

        private static Vector3 FriendPosition(IFriendStatuses friends, string id)
        {
            for (int i = 0; i < friends.Count; i++)
            {
                if (friends.Definition(i).Id == id)
                {
                    return friends.Status(i).Position;
                }
            }

            throw new InvalidOperationException($"The friends have no '{id}'.");
        }

        /// <summary>
        /// A camera looking straight at Earth through <paramref name="namePoint"/>, as far back along that line as the
        /// ground allows (so the name sits on Earth's disc), never closer than a couple of metres.
        /// </summary>
        private Camera EarthCamera(Vector3 namePoint, Vector3 earth, ITerrainQuery terrain, GameContext context)
        {
            Vector3 position = namePoint - earth * EarthShotNearest;
            for (float reach = EarthShotReach; reach > EarthShotNearest; reach -= EarthShotStep)
            {
                Vector3 candidate = namePoint - earth * reach;
                if (candidate.y >= terrain.SampleHeight(candidate.x, candidate.z) + EarthShotClearance)
                {
                    position = candidate;
                    break;
                }
            }

            position.y = Mathf.Max(position.y, terrain.SampleHeight(position.x, position.z) + EarthShotClearance);
            if (_earthCamera == null)
            {
                _earthCamera = new GameObject("EarthCaptureCamera");
                var added = _earthCamera.AddComponent<Camera>();
                added.CopyFrom(context.Get<IViewCamera>().Camera);
                added.fieldOfView = FriendShotFov;
                added.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }

            _earthCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(earth, Vector3.up));
            return _earthCamera.GetComponent<Camera>();
        }

        private static GameBootstrap FindBootstrap()
        {
            foreach (GameObject root in SceneManager.GetSceneByPath(MainScene).GetRootGameObjects())
            {
                var bootstrap = root.GetComponent<GameBootstrap>();
                if (bootstrap != null && bootstrap.Context != null)
                {
                    return bootstrap;
                }
            }

            throw new InvalidOperationException($"{MainScene} has no initialised GameBootstrap at its root.");
        }

        /// <summary>Seconds for a queued ticker line to drift fully in once nothing is in its way.</summary>
        private float TickerEntrance()
        {
            return _tuning.Ticker.GapSeconds + _tuning.Ticker.Reveal.FadeIn + 0.6f;
        }

        /// <summary>Seconds for a card to appear in full: the ticker makes way first, then the card eases in.</summary>
        private float CardEntrance()
        {
            return Mathf.Max(_tuning.MemoryCard.AppearDelay, _tuning.Ticker.Reveal.FadeOut) +
                   _tuning.MemoryCard.Reveal.FadeIn + 0.6f;
        }

        /// <summary>Seconds for the dial readout to rest and fade away.</summary>
        private float ReadoutExit()
        {
            return _tuning.DialReadout.HoldSeconds + _tuning.DialReadout.Reveal.FadeOut + 0.3f;
        }

        /// <summary>
        /// Bell's corner by the radio tower, with Bell standing in it: the scene's own Bell when the loaded save has
        /// her home, else her art model placed there for the shot.
        /// </summary>
        private Transform BellAtHome(GameContext context)
        {
            Transform corner = ActiveSceneNode(BellCornerNode);
            IFriendRoster roster = context.Get<IFriendRoster>();
            for (int i = 0; i < roster.Count; i++)
            {
                IFriendState friend = roster.Get(i);
                if (friend.Id != BellId)
                {
                    continue;
                }

                if (Vector3.ProjectOnPlane(friend.Position - corner.position, Vector3.up).magnitude >
                    BellHomeTolerance)
                {
                    _bellStandIn = (GameObject)PrefabUtility.InstantiatePrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(BellPrefabPath));
                    _bellStandIn.transform.SetPositionAndRotation(corner.position, corner.rotation);
                }

                return corner;
            }

            throw new InvalidOperationException($"The friend roster has no '{BellId}'.");
        }

        /// <summary>
        /// The part socket of the first relay mast that is still dark in the loaded save (its broken pose is the live
        /// one).
        /// </summary>
        private static Transform DarkMastSocket(IWorldAnchors anchors)
        {
            for (int i = 0; anchors.TryGet(WorldAnchorIds.RelayPrefix + i, out MoonProject.Core.WorldAnchor mast); i++)
            {
                Transform socket = NearestActiveNode(PartSocketNode, mast.Position, MastSocketReach);
                if (socket != null)
                {
                    return socket;
                }
            }

            throw new InvalidOperationException("Every relay mast is lit in the loaded save: no dark mast to capture.");
        }

        /// <summary>
        /// A relic lying loose on the ground ahead of <paramref name="view"/> and a little to the side (its art model,
        /// placed for the shot) for the Stow prompt to float over. Returns where it rests.
        /// </summary>
        private Vector3 SurfacedRelic(ITerrainQuery terrain, Camera view)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RelicPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"{RelicPrefabPath} is missing: run the Art builders.");
            }

            Transform eye = view.transform;
            Vector3 ahead = Vector3.ProjectOnPlane(eye.forward, Vector3.up).normalized;
            Vector3 spot = eye.position + ahead * RelicShotAhead + eye.right * RelicShotSide;
            spot.y = terrain.SampleHeight(spot.x, spot.z);
            _relicStandIn = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            _relicStandIn.transform.SetPositionAndRotation(spot, Quaternion.Euler(0f, RelicShotYaw, 0f));
            return spot;
        }

        /// <summary>A camera on a site's approach lane, a few metres out, looking at its heart.</summary>
        private Camera SiteCamera(MoonProject.Core.WorldAnchor site, Camera reference)
        {
            Vector3 lane = Vector3.ProjectOnPlane(site.Forward, Vector3.up).normalized;
            Vector3 position = site.Position - lane * SiteShotBack + Vector3.up * SiteShotHeight;
            _siteCamera = new GameObject("SiteCaptureCamera");
            var camera = _siteCamera.AddComponent<Camera>();
            camera.CopyFrom(reference);
            camera.fieldOfView = FriendShotFov;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            _siteCamera.transform.SetPositionAndRotation(position,
                Quaternion.LookRotation(site.Position + Vector3.up * CutHeight - position, Vector3.up));
            return camera;
        }

        /// <summary>A camera on the home side of a mast's foot, looking at its part socket.</summary>
        private Camera MastCamera(Transform socket, Camera reference)
        {
            Vector3 front = Vector3.ProjectOnPlane(socket.forward, Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, front);
            Vector3 position = socket.position + front * MastShotFront + side * MastShotSide +
                               Vector3.up * MastShotHeight;
            _mastCamera = new GameObject("MastCaptureCamera");
            var camera = _mastCamera.AddComponent<Camera>();
            camera.CopyFrom(reference);
            camera.fieldOfView = FriendShotFov;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            _mastCamera.transform.SetPositionAndRotation(position,
                Quaternion.LookRotation(socket.position + Vector3.up * MastLookHeight - position, Vector3.up));
            return camera;
        }

        /// <summary>
        /// The active node called <paramref name="name"/> in Main nearest to <paramref name="near"/>, within
        /// <paramref name="reach"/> metres, or null.
        /// </summary>
        private static Transform NearestActiveNode(string name, Vector3 near, float reach)
        {
            Transform best = null;
            float bestSq = reach * reach;
            foreach (GameObject root in SceneManager.GetSceneByPath(MainScene).GetRootGameObjects())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(false))
                {
                    float distanceSq = (node.position - near).sqrMagnitude;
                    if (node.name == name && distanceSq <= bestSq)
                    {
                        best = node;
                        bestSq = distanceSq;
                    }
                }
            }

            return best;
        }

        /// <summary>A camera beside the spot where 07 parks to tune, looking at Bell and her dial.</summary>
        private Camera BellCamera(Transform corner, Camera reference)
        {
            Vector3 position = corner.position + corner.forward * BellShotFront + corner.right * BellShotSide +
                               Vector3.up * BellShotHeight;
            _bellCamera = new GameObject("BellCaptureCamera");
            var camera = _bellCamera.AddComponent<Camera>();
            camera.CopyFrom(reference);
            camera.fieldOfView = FriendShotFov;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            _bellCamera.transform.SetPositionAndRotation(position,
                Quaternion.LookRotation(corner.position + Vector3.up * BellLookHeight - position, Vector3.up));
            return camera;
        }

        /// <summary>The active node called <paramref name="name"/> in Main (only one tower stage is live).</summary>
        private static Transform ActiveSceneNode(string name)
        {
            foreach (GameObject root in SceneManager.GetSceneByPath(MainScene).GetRootGameObjects())
            {
                Transform hit = ActiveDescendant(root.transform, name);
                if (hit != null)
                {
                    return hit;
                }
            }

            throw new InvalidOperationException($"{MainScene} has no active node named {name}.");
        }

        private static Transform ActiveDescendant(Transform node, string name)
        {
            if (!node.gameObject.activeInHierarchy)
            {
                return null;
            }

            if (node.name == name)
            {
                return node;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Transform hit = ActiveDescendant(node.GetChild(i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        /// <summary>Renders the UI at another frame size from now on (the panel lays itself out for it).</summary>
        private void Reframe(int width, int height)
        {
            RenderTexture previous = _target;
            _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _target.Create();
            _panel.targetTexture = _target;
            previous.Release();
            Object.DestroyImmediate(previous);
        }

        /// <summary>A camera a few metres from Tilly, on the side of the base, looking at her broken body.</summary>
        private Camera FriendCamera(Vector3 tilly, ITerrainQuery terrain, Camera reference)
        {
            Vector3 toBase = Vector3.ProjectOnPlane(-tilly, Vector3.up).normalized;
            Vector3 position = tilly + toBase * FriendShotDistance;
            position.y = Mathf.Max(terrain.SampleHeight(position.x, position.z), tilly.y) + FriendShotHeight;
            _friendCamera = new GameObject("TillyCaptureCamera");
            var camera = _friendCamera.AddComponent<Camera>();
            camera.CopyFrom(reference);
            camera.fieldOfView = FriendShotFov;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            _friendCamera.transform.SetPositionAndRotation(position,
                Quaternion.LookRotation(tilly + Vector3.up * 0.4f - position, Vector3.up));
            return camera;
        }

        private UISystem BuildUi(GameContext context, out FakeGameServices fakes, out SaveService save)
        {
            _fakesHost = new GameObject("CaptureFakes");
            fakes = _fakesHost.AddComponent<FakeGameServices>();
            _tower = UiTestRig.CopyCosting(UiTestRig.UpgradePath, new Recipe(2, 1, 0), new Recipe(4, 2, 1),
                new Recipe(6, 4, 2));
            _bayKit = new[]
            {
                UiTestRig.CopyCosting(UiTestRig.HoverJumpUpgradePath, new Recipe(6, 3, 0)),
                UiTestRig.CopyCosting(UiTestRig.CradleUpgradePath, new Recipe(4, 3, 0)),
                UiTestRig.CopyCosting(UiTestRig.HeadlampUpgradePath, new Recipe(3, 4, 2)),
            };
            fakes.Upgrade = _tower;
            fakes.Friend = AssetDatabase.LoadAssetAtPath<FriendDefinition>(UiTestRig.TillyPath);
            fakes.Camera = context.Get<IViewCamera>().Camera;
            fakes.Position = context.Get<IRoverState>().Position;
            fakes.TillyStatus = new FriendStatus(FriendState.Dormant, 0, 3, false, false, new Vector3(0f, 0f, 500f));
            fakes.AnnouncesPurchases = false;
            fakes.Initialize(new GameContext(context.Events, context.Input));
            fakes.SetMaterials(10, 6, 2);

            _target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _target.Create();
            _panel = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiTestRig.PanelSettingsPath));
            _panel.targetTexture = _target;
            _panel.clearColor = true;
            _panel.colorClearValue = Color.clear;
            _tuning = Object.Instantiate(AssetDatabase.LoadAssetAtPath<UiTuning>(UiTestRig.TuningPath));
            _tuning.Prompts.AddMissingDefaults();

            _uiHost = new GameObject("CaptureUI");
            _uiHost.SetActive(false);
            var document = _uiHost.AddComponent<UIDocument>();
            document.panelSettings = _panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiTestRig.UxmlPath);
            var ui = _uiHost.AddComponent<UISystem>();
            ui.Wire(document, _tuning, AssetDatabase.LoadAssetAtPath<RelicCatalog>(UiTestRig.CatalogPath),
                new[]
                {
                    AssetDatabase.LoadAssetAtPath<TextAsset>(UiTestRig.EnglishPath),
                    AssetDatabase.LoadAssetAtPath<TextAsset>(UiTestRig.VietnamesePath),
                }, AssetDatabase.LoadAssetAtPath<WordmarkShape>(UiTestRig.WordmarkPath),
                AssetDatabase.LoadAssetAtPath<TextAsset>(UiTestRig.CreditsPath));
            ui.QuitAction = () => { };
            _uiHost.SetActive(true);

            _slot = BootstrapHarness.NewTestSlot();
            save = new SaveService(SaveService.DefaultDirectory, _slot);
            ui.Initialize(new UiServices(context.Events, context.Input, fakes, fakes, context.Get<IAudioSettings>(),
                context.Get<ILookSettings>(), context.Get<IFeelSettings>(), save, fakes, fakes, fakes, fakes, fakes, fakes, fakes, fakes, fakes,
                fakes, context.Get<IGameFlow>(), new SettingsFile(SaveService.DefaultDirectory, _slot), fakes, fakes,
                fakes, fakes, fakes, context.Get<IWorldLayout>(), fakes, fakes.Hold, fakes.Catalog, fakes.Dock, fakes.Home, fakes.Transfer,
                fakes.Equipment));
            save.Load();
            return ui;
        }

        private IEnumerator Capture(Camera camera, string folder, string name)
        {
            yield return null;
            int width = _target.width;
            int height = _target.height;
            Texture2D world = FrameCapture.Render(camera, width, height);
            var ui = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            ui.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            ui.Apply(false);
            RenderTexture.active = previous;
            try
            {
                Color32[] background = world.GetPixels32();
                Color32[] overlay = ui.GetPixels32();
                for (int i = 0; i < background.Length; i++)
                {
                    background[i] = Over(overlay[i], background[i]);
                }

                world.SetPixels32(background);
                world.Apply(false);
                string path = Path.Combine(folder, name + ".png");
                FrameCapture.WritePng(world, path);
                Debug.Log($"[ui-capture] {path}");
            }
            finally
            {
                Object.Destroy(world);
                Object.Destroy(ui);
            }
        }

        /// <summary>UI Toolkit writes premultiplied colour: result = ui + world x (1 - ui alpha).</summary>
        private static Color32 Over(Color32 ui, Color32 world)
        {
            int keep = 255 - ui.a;
            return new Color32(
                (byte)Mathf.Min(255, ui.r + world.r * keep / 255),
                (byte)Mathf.Min(255, ui.g + world.g * keep / 255),
                (byte)Mathf.Min(255, ui.b + world.b * keep / 255),
                255);
        }
#endif

        private static void Submit(VisualElement target)
        {
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = target;
                target.SendEvent(submit);
            }
        }

        private IEnumerator Tap(ButtonControl button)
        {
            Press(button, queueEventOnly: true);
            yield return null;
            Release(button, queueEventOnly: true);
            yield return null;
        }
    }
}
