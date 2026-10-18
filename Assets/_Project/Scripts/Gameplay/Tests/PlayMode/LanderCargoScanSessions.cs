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
    /// The lander's open crate from where 07 arrives (opening-v2 B3): stopped where the cable ends at the service port
    /// and turned toward the crate, both pad fragments lie in scan reach and inside the camera cone, nothing but 07
    /// itself stands between them and its lens or the camera, they show above the crate's shell, and each is the one
    /// the scan picks when the view rests on it (the autoplay timed out at B3 when they lay 17 cm apart inside a
    /// collider that wrapped them).
    /// </summary>
    public sealed class LanderCargoScanSessions : InputTestFixture
    {
        private const string CrateId = "supply.lander.open";
        private const string PortName = "ServicePort";
        private const float PortBack = 3.5f;
        private const float ChaseBack = 7.5f;
        private const float ChaseHeight = 1.9f;
        private const float CrateLookHeight = 0.5f;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;

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
        public IEnumerator BothCargoFragments_AreScanAcquirable_From07sArrivalAtThePort()
        {
            _fixture = GameplayFixture.Boot(_controls, startKit: true);
            yield return null;
            yield return null;

            Transform crate = _fixture.Gameplay.Opening.Caches.RootOf(CrateId);
            Vector3 lander = _fixture.World.Anchor(WorldAnchorIds.PlaceLander).Position;
            Vector3 port = Port().position;
            Vector3 outward = Flat(port - lander).normalized;
            Vector3 stop = port + outward * PortBack;
            Vector3 toCrate = Flat(crate.position - stop).normalized;
            _fixture.Rover.Place(new Vector3(stop.x, 0f, stop.z), Quaternion.LookRotation(toCrate).eulerAngles.y);
            Vector3 eye = stop - toCrate * ChaseBack + Vector3.up * ChaseHeight;
            _fixture.Rover.Aim(eye, crate.position + Vector3.up * CrateLookHeight);
            yield return null;

            ScanField scans = _fixture.Gameplay.Scans;
            float shellTop = crate.GetComponent<Collider>().bounds.max.y;
            for (int i = 0; i < 2; i++)
            {
                ScanFragment fragment = scans.Find("ruin." + ScanTuning.LanderCargoId + "." + i);
                Assert.IsNotNull(fragment, $"cargo fragment {i} keeps its id");
                Vector3 point = fragment.Point;
                Assert.LessOrEqual(Flat(point - stop).magnitude, _fixture.ScanTuning.Reach,
                    $"{fragment.FragmentId} lies in scan reach of 07 at the port");
                Assert.IsTrue(scans.InView(point, out _), $"{fragment.FragmentId} lies inside the camera cone");
                Assert.Greater(point.y, shellTop, $"{fragment.FragmentId} shows above the crate's shell");
                AssertClear(eye, point, "the camera", fragment.FragmentId);
                AssertClear(_fixture.Rover.ScanOrigin.position, point, "07's lens", fragment.FragmentId);
            }

            for (int i = 0; i < 2; i++)
            {
                ScanFragment fragment = scans.Find("ruin." + ScanTuning.LanderCargoId + "." + i);
                _fixture.Rover.Aim(eye, fragment.Point);
                yield return null;
                Assert.AreSame(fragment, scans.LookedAt(out _),
                    $"the view resting on {fragment.FragmentId} picks it, not its neighbour");
                Assert.AreSame(fragment, scans.Candidate, $"the brackets go to {fragment.FragmentId}");
            }
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

        private void AssertClear(Vector3 from, Vector3 to, string what, string fragmentId)
        {
            foreach (RaycastHit hit in Physics.RaycastAll(from, to - from, Vector3.Distance(from, to),
                         ~0, QueryTriggerInteraction.Ignore))
            {
                Assert.IsTrue(hit.collider.transform.IsChildOf(_fixture.Rover.transform),
                    $"{hit.collider.name} stands between {what} and {fragmentId}");
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }
    }
}
