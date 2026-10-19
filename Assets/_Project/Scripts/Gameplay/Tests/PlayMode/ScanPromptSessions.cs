using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The scan prompt and the target it is for (owner build 95b1dd3a: "[E] Scan" with nothing to scan in sight):
    /// a fragment in reach that the view does not rest on, or one out of reach, is no candidate and shows no mark; the
    /// view resting on one in reach makes it the candidate and puts the wire box on it; a fragment already scanned is
    /// no candidate even when the view rests on it.
    /// </summary>
    public sealed class ScanPromptSessions : InputTestFixture
    {
        private const string FragmentId = "ruin." + ScanTuning.LanderCargoId + ".0";
        private const string SiblingId = "ruin." + ScanTuning.LanderCargoId + ".1";
        private const string CrateId = "supply.lander.open";
        private const string PortName = "ServicePort";
        private const float PortBack = 3.5f;
        private const float ChaseBack = 7.5f;
        private const float ChaseHeight = 1.9f;
        private const float FarOutMetres = 40f;
        private const float MarkWaitSeconds = 3f;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Vector3 _stop;
        private Vector3 _toCrate;
        private Vector3 _eye;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator AFragmentBehindTheView_OrOutOfReach_IsNoCandidateAndHasNoMark()
        {
            yield return Boot();
            Vector3 stop = _stop;
            Vector3 toCrate = _toCrate;
            Vector3 eye = _eye;
            ScanField scans = _fixture.Gameplay.Scans;
            ScanFragment fragment = scans.Find(FragmentId);
            Assert.LessOrEqual(Flat(fragment.Point - stop).magnitude, _fixture.ScanTuning.Reach,
                "the fragment lies in reach of 07 at the port");

            _fixture.Rover.Aim(eye, eye - toCrate * ChaseBack);
            yield return null;
            yield return null;
            Assert.IsNull(scans.Candidate, "with the view turned away the fragment in reach is no candidate");
            yield return new WaitForSeconds(_fixture.ScanTuning.TargetMarkFadeSeconds + 0.2f);
            Assert.IsNull(_fixture.Gameplay.ScanMark.Shown, "and nothing is marked");

            Vector3 far = stop - toCrate * FarOutMetres;
            _fixture.Rover.Place(new Vector3(far.x, 0f, far.z), Quaternion.LookRotation(toCrate).eulerAngles.y);
            _fixture.Rover.Aim(far - toCrate * ChaseBack + Vector3.up * ChaseHeight, fragment.Point);
            yield return null;
            yield return null;
            Assert.IsNull(scans.Candidate, "a fragment out of reach is no candidate even when the view is on it");
        }

        [UnityTest]
        public IEnumerator TheViewResting_OnAFragmentInReach_MakesItTheCandidateAndMarksIt()
        {
            yield return Boot();
            Vector3 eye = _eye;
            ScanField scans = _fixture.Gameplay.Scans;
            ScanFragment fragment = scans.Find(FragmentId);

            _fixture.Rover.Aim(eye, fragment.Point);
            yield return null;
            yield return null;
            Assert.AreSame(fragment, scans.Candidate, "the view on the fragment makes it the candidate");
            float waited = 0f;
            while (_fixture.Gameplay.ScanMark.Visible < 1f && waited < MarkWaitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.AreSame(fragment, _fixture.Gameplay.ScanMark.Shown, "the wire box rests on the candidate");
            Assert.AreEqual(1f, _fixture.Gameplay.ScanMark.Visible, 1e-4f, "and has faded all the way in");
        }

        [UnityTest]
        public IEnumerator AFragmentAlreadyScanned_IsNoCandidateEvenWithTheViewOnIt()
        {
            yield return Boot();
            Vector3 eye = _eye;
            ScanField scans = _fixture.Gameplay.Scans;
            ScanFragment fragment = scans.Find(FragmentId);
            ScansSaveData data = scans.Capture();
            data.scanned = new[] { FragmentId, SiblingId };
            scans.Restore(data);

            _fixture.Rover.Aim(eye, fragment.Point);
            yield return null;
            yield return null;
            Assert.IsNull(scans.Candidate, "with both fragments scanned there is nothing to prompt for");
        }

        private IEnumerator Boot()
        {
            _fixture = GameplayFixture.Boot(_controls, startKit: true);
            yield return null;
            yield return null;

            Transform crate = _fixture.Gameplay.Opening.Caches.RootOf(CrateId);
            Vector3 lander = _fixture.World.Anchor(WorldAnchorIds.PlaceLander).Position;
            Vector3 port = Port().position;
            Vector3 outward = Flat(port - lander).normalized;
            _stop = port + outward * PortBack;
            _toCrate = Flat(crate.position - _stop).normalized;
            _fixture.Rover.Place(new Vector3(_stop.x, 0f, _stop.z), Quaternion.LookRotation(_toCrate).eulerAngles.y);
            _eye = _stop - _toCrate * ChaseBack + Vector3.up * ChaseHeight;
            yield return null;
        }

        private Transform Port()
        {
            foreach (Transform node in _fixture.Gameplay.transform.root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == PortName)
                {
                    return node;
                }
            }

            Assert.Fail("The fixture has no lander service port.");
            return null;
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }
    }
}
