using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Scripted sessions for the places that hold scan fragments (D2, docs/design/0.5.5-owner-feedback.md §1.4) on the
    /// real components behind the fake rover: <see cref="IScanSites"/> lists every place (a derelict inside a wreck as
    /// its wreck), tells a place's reach once per save and its emptying once; listening answers a derelict with
    /// fragments left and falls quiet once it is emptied, and the scrap field never answers.
    /// </summary>
    public sealed class ScanSiteSessions : InputTestFixture
    {
        private const string CargoCradle = "rover.cargo_cradle";
        private const string CellPack = "rover.cell_pack";
        private const string Garage = "site.garage";
        private const string Kestrel = "site.kestrel";

        /// <summary>The haul bot's derelict id (it stands inside the drill site).</summary>
        private const string HaulBot = "derelict.haul_bot";

        /// <summary>Seconds a held press may take to be seen (a slow first frame must not eat it).</summary>
        private const float PressTimeout = 1f;

        /// <summary>Metres 07 parks from a fragment, looking at it.</summary>
        private const float Park = 4f;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TheScanSites_ListEveryPlace_TellItsReachOnce_AndItsEmptying_AndALoadKeepsThem()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            var sites = _fixture.Bootstrap.Context.Get<IScanSites>();
            Assert.AreSame(_fixture.Gameplay.Scans, sites, "registered for the chart and the place cards");
            Assert.AreEqual(7, sites.Count, "three wrecks, the drill (its haul bot), 06, the Mule and the lip");
            Assert.AreEqual(-1, sites.IndexOf(HaulBot), "the haul bot counts as its wreck");
            Assert.AreEqual(3, sites.FragmentCount(sites.IndexOf("site.drill")), "Boost Coils on the haul bot");
            Assert.GreaterOrEqual(sites.IndexOf(WorldAnchorIds.CanyonLip), 0, "the jumper at the chasm's lip");
            int garage = sites.IndexOf(Garage);
            Assert.AreEqual(2, sites.FragmentCount(garage));
            Assert.AreEqual(2, sites.Left(garage));
            Assert.IsFalse(sites.IsKnown(garage), "not heard, not reached");
            Assert.AreEqual(2, sites.SetSize(CargoCradle));
            Assert.AreEqual(0, sites.ScannedOf(CargoCradle));
            Assert.AreEqual(0, sites.SetSize("radio_tower"), "no set");

            Vector3 at = sites.Position(garage);
            _fixture.Rover.Place(new Vector3(at.x, 0f, at.z), 0f);
            yield return null;
            yield return null;
            Assert.AreEqual(1, _fixture.Events.ScanSiteReached.Count, "within the garage: told once");
            ScanSiteReached reached = _fixture.Events.ScanSiteReached[0].Value;
            Assert.AreEqual(Garage, reached.PlaceId);
            Assert.AreEqual(2, reached.Left, "2 fragments to scan");
            Assert.AreEqual(2, reached.Count);
            Assert.IsTrue(sites.IsKnown(garage));
            _fixture.Rover.Place(Vector3.zero, 0f);
            yield return null;
            _fixture.Rover.Place(new Vector3(at.x, 0f, at.z), 0f);
            yield return null;
            yield return null;
            Assert.AreEqual(1, _fixture.Events.ScanSiteReached.Count, "never twice");

            yield return ScanAll(Garage);
            Assert.AreEqual(1, _fixture.Events.ScanSiteEmptied.Count, "the last fragment empties the place, once");
            Assert.AreEqual(Garage, _fixture.Events.ScanSiteEmptied[0].Value.PlaceId);
            Assert.AreEqual(0, sites.Left(garage));
            Assert.AreEqual(2, sites.ScannedOf(CargoCradle));

            Vector3 kestrel = sites.Position(sites.IndexOf(Kestrel));
            _fixture.Rover.Place(new Vector3(kestrel.x, 0f, kestrel.z), 0f);
            yield return null;
            yield return null;
            Assert.AreEqual(2, _fixture.Events.ScanSiteReached.Count, "the Kestrel's lens stack: told once");
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<ISaveService>().SaveNow());
            _fixture.Dispose(true);
            yield return null;

            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            sites = _fixture.Bootstrap.Context.Get<IScanSites>();
            Assert.AreEqual(0, sites.Left(sites.IndexOf(Garage)), "emptied across a load");
            Assert.IsTrue(sites.IsKnown(sites.IndexOf(Kestrel)), "reached across a load");
            _fixture.Rover.Place(new Vector3(kestrel.x, 0f, kestrel.z), 0f);
            yield return null;
            yield return null;
            Assert.IsEmpty(_fixture.Events.ScanSiteReached, "a place already told stays told");
            Assert.IsEmpty(_fixture.Events.ScanSiteEmptied, "a load is silent");
        }

        [UnityTest]
        public IEnumerator Listening_AnswersADerelictWithFragmentsLeft_AndItFallsQuietOnceEmptied()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SalvageField salvage = _fixture.Gameplay.Salvage;
            SalvageSite elder = salvage.FindDerelict(WorldAnchorIds.Micro06);
            SalvageSite scrap = salvage.FindDerelict(SalvageField.ScrapFieldId);
            var sites = _fixture.Bootstrap.Context.Get<IScanSites>();
            int place = sites.IndexOf(WorldAnchorIds.Micro06);
            Assert.IsTrue(elder.IsPickedClean, "06 has no piece to cut");
            Assert.IsTrue(elder.AnswersSonar, "but its battery bay holds Cell Pack II's fragments");
            Assert.IsTrue(salvage.StandsAlone(elder));
            Assert.IsFalse(salvage.StandsAlone(scrap), "the scrap field is a trail, not a place");
            Assert.IsFalse(salvage.StandsAlone(salvage.FindDerelict(HaulBot)), "inside the drill");

            _fixture.Rover.Place(elder.Position + new Vector3(0f, 0f, -30f), 0f);
            _fixture.PowerTo(1);
            yield return Waits.Until(() => _fixture.Events.SonarPinged.Count > 0,
                _fixture.SonarTuning.RestSeconds + 1f);
            Assert.Greater(_fixture.Events.SonarPinged.Count, 0, "07 listens once it has rested at level 1");
            yield return Waits.Until(() => elder.Discovered,
                _fixture.SonarTuning.SweepDuration + _fixture.SonarTuning.AnswerLag + 1f);
            Assert.IsTrue(elder.Discovered, "06 answered");
            Assert.IsTrue(Answered(WorldAnchorIds.Micro06), "with its own id: UI names it micro.06.name");
            Assert.IsFalse(Answered(SalvageField.ScrapFieldId), "the scrap field never answers");
            Assert.IsTrue(sites.IsKnown(place), "heard: the chart may bracket it");

            _fixture.Gameplay.Scans.Restore(new ScansSaveData
            {
                scanned = new[] { CellPack + ".0", CellPack + ".1", CellPack + ".2" },
            });
            Assert.AreEqual(0, sites.Left(place));
            Assert.IsFalse(elder.AnswersSonar, "emptied: it falls quiet");
        }

        private bool Answered(string siteId)
        {
            foreach (EventRecorder.Timed<SiteAnswered> answer in _fixture.Events.SiteAnswered)
            {
                if (answer.Value.SiteId == siteId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Scans every fragment at <paramref name="placeId"/>, one hold of E each.</summary>
        private IEnumerator ScanAll(string placeId)
        {
            foreach (ScanFragment fragment in _fixture.Gameplay.Scans.Fragments)
            {
                if (fragment.HostId != placeId || fragment.Scanned)
                {
                    continue;
                }

                yield return Face(fragment);
                Press(_keyboard.eKey);
                yield return Waits.Until(() => fragment.Scanned,
                    _fixture.ScanTuning.Seconds(fragment.Size) + PressTimeout);
                Release(_keyboard.eKey);
                yield return null;
                Assert.IsTrue(fragment.Scanned, fragment.FragmentId);
            }
        }

        /// <summary>07 parks <see cref="Park"/> m from the fragment, its view on it, away from its host.</summary>
        private IEnumerator Face(ScanFragment fragment)
        {
            Vector3 point = fragment.Point;
            Vector3 away = point - fragment.Root.parent.parent.position;
            away.y = 0f;
            away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.back;
            Vector3 park = point + away * Park;
            _fixture.Rover.Place(new Vector3(park.x, 0f, park.z), Mathf.Atan2(-away.x, -away.z) * Mathf.Rad2Deg);
            _fixture.Rover.Aim(point + away * (Park + 4f) + Vector3.up * 3f, point);
            yield return null;
            yield return null;
        }
    }
}
