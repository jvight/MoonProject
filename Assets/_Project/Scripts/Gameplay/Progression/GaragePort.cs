using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kenji's garage service port (docs/design/first-hour-v2.md §3), Art's GaragePort standing in the garage behind
    /// its painted "07" pad. The garage's last cut opens it: its place card, its pad lit. Parked on the pad, 07 has its
    /// spare tether ring fitted round its eye, through 07's equipment (<see cref="PartFitted"/>, 0.5.5 §3.5: the
    /// Tether ability, asleep until then). Then the seal of Kenji's "07 · SPARE" crate on his bench glows (0.5.5 §2:
    /// Ro's tape deck is a station spare part, not a keepsake): cut open, the deck rises to the crate's mouth, a press
    /// of E there sends it gliding onto the port's tray, and parked on the pad again 07 has it fitted the same way.
    /// Before the ring, a loose keepsake near 07 gives one line, once: the tether is gone and Kenji kept a spare. A
    /// save that already used the tether (a keepsake home, or the Cargo Cradle) has the ring.
    /// </summary>
    public sealed class GaragePort : IDisposable
    {
        /// <summary>The garage's node where Kenji's spare crate stands (Art's site contract).</summary>
        public const string SpareCrateSocket = "SpareCrateSocket";

        /// <summary>The save id of the spare crate.</summary>
        public const string SpareCrateId = "container.garage.spare_crate";

        /// <summary>The deck's name as the pickup prompt says it (its equipment name).</summary>
        private const string DeckNoun = "kit.tape_deck.name";

        private const float ApproachRadius = 22f;
        private const float LeaveRadius = 40f;
        private const float PortInstallSeconds = 2.4f;
        private const float ArmReachShare = 0.42f;
        private const float ArmSeatShare = 0.65f;
        private const float RingSocketHeight = 0.72f;
        private const float DeckSocketHeight = 0.52f;
        private const int SparkBursts = 3;
        private const int SparkCountPerBurst = 14;
        private const float SparkInterval = 0.14f;

        private enum DeckPhase
        {
            Sealed,
            Rising,
            InTheCrate,
            Gliding,
            OnTheTray,
            Installing,
            Fitted,
        }

        private readonly ProgressionTuning _tuning;
        private readonly SalvageSite _garage;
        private readonly RelicField _relics;
        private readonly IRoverAbilities _abilities;
        private readonly RoverEquipment _equipment;
        private readonly TetherSystem _tether;
        private readonly IRoverState _rover;
        private readonly ISaveService _save;
        private readonly EventBus _events;
        private readonly InputReader _input;
        private readonly PickupReach _pickups;
        private readonly FriendTuning _friends;
        private readonly Transform _model;
        private readonly Transform _tray;
        private readonly Transform _armTip;
        private readonly Vector3 _armRest;
        private readonly ParticleSystem _sparks;
        private readonly GameObject _pad;
        private readonly Vector3 _padCentre;
        private readonly SealedContainers _containers;
        private readonly SealedContainer _crate;
        private readonly Transform _deck;
        private readonly Vector3 _deckScale;
        private DeckPhase _deckPhase;
        private float _since;
        private float _glide;
        private float _parked;
        private float _ringInstallElapsed;
        private float _deckInstallElapsed;
        private bool _ringInstalling;
        private bool _ringSparksFired;
        private bool _deckSparksFired;
        private bool _atGarage;
        private bool _ringFitted;
        private bool _deckFitted;
        private bool _placeSeen;
        private bool _tetherHinted;

        /// <exception cref="InvalidOperationException">
        /// The garage site, Art's port, its spare crate or the deck's model is missing.
        /// </exception>
        public GaragePort(Transform parent, GameplayServices services, SalvageField salvage, RelicField relics,
            IRoverAbilities abilities, ProgressionTuning tuning, FriendTuning friends, TetherSystem tether,
            RoverEquipment equipment, SealedContainers containers)
        {
            _tether = tether != null ? tether : throw new ArgumentNullException(nameof(tether));
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            if (services == null || salvage == null || containers == null)
            {
                throw new ArgumentNullException(services == null ? nameof(services)
                    : salvage == null ? nameof(salvage) : nameof(containers));
            }

            _relics = relics != null ? relics : throw new ArgumentNullException(nameof(relics));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            _rover = services.Rover;
            _save = services.Save;
            _events = services.Events;
            _input = services.Input;
            _pickups = services.Pickups;
            _garage = salvage.Find(tuning.PortSite) ?? throw new InvalidOperationException(
                $"Kenji's garage port stands at '{tuning.PortSite}', which is no salvage site.");
            Transform site = _garage.Root;
            _model = Object.Instantiate(services.Visuals.GaragePort, parent).transform;
            _model.name = "GaragePort";
            _model.SetPositionAndRotation(site.TransformPoint(tuning.PortLocal),
                site.rotation * Quaternion.Euler(0f, tuning.PortYaw, 0f));
            _tray = _model.Find(GameplayVisuals.PortTrayNode) ?? throw new InvalidOperationException(
                $"Art's GaragePort has no '{GameplayVisuals.PortTrayNode}' node.");
            Transform armTip = _model.Find(GameplayVisuals.PortArmTipNode);
            if (armTip == null)
            {
                armTip = new GameObject(GameplayVisuals.PortArmTipNode).transform;
                armTip.SetParent(_model, false);
                armTip.localPosition = new Vector3(0.9f, 1.35f, 1.32f);
            }

            _armTip = armTip;
            _armRest = _armTip.localPosition;
            _sparks = CreatePortSparks(_armTip, services.Visuals.Spark);
            _padCentre = site.TransformPoint(tuning.PortPadLocal);
            _pad = Object.Instantiate(services.Visuals.PadFor(tuning.PortPadRadius), parent);
            _pad.name = "GaragePortPad";
            _pad.transform.SetPositionAndRotation(_padCentre, site.rotation);
            _pad.SetActive(false);
            _containers = containers;
            _crate = StandTheCrate(site, services.Visuals);
            _deck = Object.Instantiate(services.Visuals.TapeDeck, parent).transform;
            _deck.name = "TapeDeck";
            _deckScale = _deck.localScale;
            _deck.SetPositionAndRotation(_crate.ItemSocket.position, _crate.ItemSocket.rotation);
            SalvageSiteBuilder.SetLayer(_deck, Layers.Pickup);
            _deck.gameObject.SetActive(false);
        }

        /// <summary>True while 07 is at the garage site.</summary>
        public bool AtGarage => _atGarage;

        /// <summary>True once the garage's last cut has opened the port.</summary>
        public bool IsOpen => _garage.IsPickedClean;

        /// <summary>True once the port has fitted 07's tether ring.</summary>
        public bool RingFitted => _ringFitted;

        /// <summary>True once the port has fitted Ro's tape deck on 07.</summary>
        public bool DeckFitted => _deckPhase == DeckPhase.Fitted;

        /// <summary>True while the port's service arm is reaching out and welding a piece onto 07.</summary>
        public bool Installing => _ringInstalling || _deckPhase == DeckPhase.Installing;

        /// <summary>True while the deck waits on the port's tray for 07 to park.</summary>
        public bool DeckOnTheTray => _deckPhase == DeckPhase.OnTheTray;

        /// <summary>True while the deck rests at the open crate's mouth, to be taken with E or Tether.</summary>
        public bool DeckInTheCrate => _deckPhase == DeckPhase.InTheCrate;

        /// <summary>Kenji's "07 · SPARE" crate on his bench, the deck sealed inside.</summary>
        public SealedContainer SpareCrate => _crate;

        /// <summary>Where the deck rests at the open crate's mouth.</summary>
        public Vector3 DeckPosition => _deck.position;

        /// <summary>Where the port's service arm tip is in world space right now.</summary>
        public Vector3 ArmTipPosition => _armTip.position;

        /// <summary>Where the port's service arm tip rests when folded.</summary>
        public Vector3 ArmRestPosition => _model.TransformPoint(_armRest);

        /// <summary>Live weld sparks emitted by the port's service arm.</summary>
        public int SparkCount => _sparks != null ? _sparks.particleCount : 0;

        /// <summary>
        /// True while 07 has stood parked on the pad: its port parts can be taken off or refitted here (0.5.5 §3.5).
        /// </summary>
        public bool Parked => _parked >= _tuning.PortParkSeconds;

        /// <summary>The middle of the port's pad, where 07 parks.</summary>
        public Vector3 PadCentre => _padCentre;

        /// <summary>Where a piece 07 sets down on the tray rests.</summary>
        public Vector3 TrayPosition => _tray.position;

        /// <summary>True once any relic in the world has been surfaced from the ground.</summary>
        public bool AnyRelicLoose
        {
            get
            {
                for (int i = 0; i < _relics.Relics.Count; i++)
                {
                    if (!_relics.Relics[i].CanBeLifted)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>The exact current step of the garage beat (a)-(d), or empty once the deck is fitted.</summary>
        public string StepLine()
        {
            if (DeckFitted)
            {
                return string.Empty;
            }

            if (!RingFitted)
            {
                return !IsOpen ? ProgressionKeys.GarageScrapKey(_garage.Remaining) : ProgressionKeys.NextPort;
            }

            return !_crate.Opened ? ProgressionKeys.NextGarageCrate : ProgressionKeys.NextGarageDeck;
        }

        public GaragePortSaveData Capture()
        {
            return new GaragePortSaveData
            {
                ringFitted = _ringFitted || _ringInstalling,
                deckFitted = _deckFitted || _deckPhase == DeckPhase.Installing,
                placeSeen = _placeSeen,
                tetherHinted = _tetherHinted,
            };
        }

        public void Restore(GaragePortSaveData data)
        {
            _ringFitted = data != null && data.ringFitted;
            _deckFitted |= data != null && data.deckFitted;
            _placeSeen = data != null && data.placeSeen;
            _tetherHinted = data != null && data.tetherHinted;
            _ringInstalling = false;
            _ringInstallElapsed = 0f;
            _deckInstallElapsed = 0f;
            _armTip.localPosition = _armRest;
        }

        /// <summary>
        /// A save from before the spare crate fitted Ro's deck as the cassette_player keepsake: it stays fitted
        /// (<see cref="TapeDeckMigration"/>). Called while the relics are restored, before this section's own.
        /// </summary>
        public void AdoptLegacyDeck()
        {
            _deckFitted = true;
        }

        /// <summary>
        /// The save is applied: the ring (also for a save that already used the tether) and the deck go back on 07
        /// at once, and the tether wakes with the ring. A fitted deck's crate stands open; a deck taken out but not
        /// yet fitted waits on the tray.
        /// </summary>
        public void Announce()
        {
            _ringFitted |= UsedTheTether();
            if (_ringFitted)
            {
                _equipment.RestoreOwned(RoverKitPiece.TetherRing);
            }

            if (_deckFitted)
            {
                _deckPhase = DeckPhase.Fitted;
                _equipment.RestoreOwned(RoverKitPiece.TapeDeck);
                _containers.StandOpen(_crate.Id);
            }
            else if (_crate.Opened)
            {
                _deckPhase = DeckPhase.OnTheTray;
                ShowOnTheTray();
            }
        }

        public void Step(float deltaTime)
        {
            float now = Time.time;
            float toGarage = Flat(_rover.Position - _garage.Position).magnitude;
            if (toGarage <= ApproachRadius)
            {
                _atGarage = true;
            }
            else if (toGarage > LeaveRadius)
            {
                _atGarage = false;
            }

            bool open = IsOpen;
            float toPad = Flat(_rover.Position - _padCentre).magnitude;
            if (open && !_placeSeen && toPad <= _tuning.PortPlaceRadius)
            {
                _placeSeen = true;
            }

            bool parked = open && toPad <= _tuning.PortPadRadius && _rover.Speed <= _tuning.PortParkSpeed;
            _parked = parked ? _parked + deltaTime : 0f;
            if (open && !_ringFitted && !_ringInstalling && Parked)
            {
                BeginRingInstall();
            }

            if (_ringInstalling)
            {
                StepRingInstall(deltaTime);
            }

            StepDeck(now, deltaTime);
            HintTheTether();
            _pad.SetActive(open &&
                           (!_ringFitted || _deckPhase == DeckPhase.OnTheTray || _deckPhase == DeckPhase.Installing));
        }

        public void Dispose()
        {
            if (_model != null)
            {
                Object.Destroy(_model.gameObject);
            }

            if (_pad != null)
            {
                Object.Destroy(_pad);
            }

            if (_deck != null)
            {
                Object.Destroy(_deck.gameObject);
            }
        }

        private SealedContainer StandTheCrate(Transform site, GameplayVisuals visuals)
        {
            Transform socket = site.Find(SpareCrateSocket) ?? throw new InvalidOperationException(
                $"'{site.name}' has no '{SpareCrateSocket}' for Kenji's spare crate (Art's site contract, 0.5.5 §2).");
            GameObject prefab = visuals.Container(ContainerKinds.SpareCrate) ?? throw new InvalidOperationException(
                $"Art's '{ContainerKinds.SpareCrate}' is not in GameplayVisuals (run Gameplay/Materials).");
            Transform root = Object.Instantiate(prefab, socket).transform;
            root.name = ContainerKinds.SpareCrate;
            root.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            SealedContainer crate = SealedContainer.Read(SpareCrateId, ContainerKinds.Of(ContainerKinds.SpareCrate),
                root, SealedContainer.LidPath, SealedContainer.ItemSocketPath,
                () => _ringFitted && _deckPhase == DeckPhase.Sealed, out string problem);
            if (crate == null)
            {
                throw new InvalidOperationException(problem);
            }

            _containers.Add(crate);
            return crate;
        }

        private void BeginRingInstall()
        {
            _ringInstalling = true;
            _ringInstallElapsed = 0f;
            _ringSparksFired = false;
            _equipment.FitItself(RoverKitPiece.TetherRing);
        }

        private void StepRingInstall(float deltaTime)
        {
            _ringInstallElapsed += deltaTime;
            float t = Mathf.Clamp01(_ringInstallElapsed / PortInstallSeconds);
            Vector3 socket = _rover.Position + Vector3.up * RingSocketHeight;
            Vector3 restWorld = _model.TransformPoint(_armRest);
            if (t < ArmReachShare)
            {
                _armTip.position = Vector3.Lerp(restWorld, socket, Ease.InOutSine(t / ArmReachShare));
            }
            else if (t < ArmSeatShare)
            {
                _armTip.position = socket;
            }
            else
            {
                if (!_ringSparksFired)
                {
                    _ringSparksFired = true;
                    BurstSparks();
                }

                float u = Ease.InOutSine((t - ArmSeatShare) / (1f - ArmSeatShare));
                _armTip.position = Vector3.Lerp(socket, restWorld, u);
            }

            if (t >= 1f)
            {
                _ringInstalling = false;
                _ringFitted = true;
                _armTip.localPosition = _armRest;
                _save.SaveNow();
            }
        }

        /// <summary>
        /// The crate cut open, the deck rises to its mouth; a press of E or Tether sends it gliding onto the tray, and
        /// 07 parked on the pad has the arm carry and weld it onto 07's left flank.
        /// </summary>
        private void StepDeck(float now, float deltaTime)
        {
            switch (_deckPhase)
            {
                case DeckPhase.Sealed:
                    if (_crate.StandsOpen)
                    {
                        _deckPhase = DeckPhase.Rising;
                        _since = now;
                        _deck.gameObject.SetActive(true);
                    }

                    break;
                case DeckPhase.Rising:
                    float rise = Mathf.Clamp01((now - _since) / _tuning.PartAppearSeconds);
                    _deck.position = Vector3.Lerp(_crate.ItemSocket.position, _crate.PresentPoint, Ease.OutCubic(rise));
                    if (rise >= 1f)
                    {
                        _deckPhase = DeckPhase.InTheCrate;
                    }

                    break;
                case DeckPhase.InTheCrate:
                    if (_pickups.Offer(Time.frameCount, _input.ExcavateHeld || _input.TetherHeld, PickupReach.Port, 0,
                            _deck.position, _rover.Position, _friends.PartReachRadius, DeckNoun))
                    {
                        _deckPhase = DeckPhase.Gliding;
                        _glide = 0f;
                        PickupReach.Lifted(_events, _deck.position, _tuning.PortTrayGlideSeconds);
                    }

                    break;
                case DeckPhase.Gliding:
                    _glide += deltaTime;
                    float progress = Mathf.Clamp01(_glide / _tuning.PortTrayGlideSeconds);
                    float eased = Ease.InOutSine(progress);
                    _deck.SetPositionAndRotation(Vector3.Lerp(_crate.PresentPoint, _tray.position, eased),
                        Quaternion.Slerp(_crate.ItemSocket.rotation, _tray.rotation, eased));
                    if (progress >= 1f)
                    {
                        _deckPhase = DeckPhase.OnTheTray;
                    }

                    break;
                case DeckPhase.OnTheTray:
                    if (Parked)
                    {
                        _deckPhase = DeckPhase.Installing;
                        _deckInstallElapsed = 0f;
                        _deckSparksFired = false;
                        _equipment.FitItself(RoverKitPiece.TapeDeck);
                    }

                    break;
                case DeckPhase.Installing:
                    _deckInstallElapsed += deltaTime;
                    float installT = Mathf.Clamp01(_deckInstallElapsed / PortInstallSeconds);
                    Vector3 deckSocket = _rover.Position + Vector3.up * DeckSocketHeight;
                    Vector3 restWorld = _model.TransformPoint(_armRest);
                    if (installT < ArmReachShare)
                    {
                        float u = Ease.InOutSine(installT / ArmReachShare);
                        _armTip.position = Vector3.Lerp(restWorld, _tray.position, u);
                    }
                    else if (installT < ArmSeatShare)
                    {
                        float u = Ease.InOutSine((installT - ArmReachShare) / (ArmSeatShare - ArmReachShare));
                        Vector3 carry = Vector3.Lerp(_tray.position, deckSocket, u);
                        _armTip.position = carry;
                        _deck.position = carry;
                        _deck.localScale = _deckScale * Mathf.Lerp(1f, 0.35f, u);
                    }
                    else
                    {
                        if (!_deckSparksFired)
                        {
                            _deckSparksFired = true;
                            _deck.gameObject.SetActive(false);
                            _deck.localScale = _deckScale;
                            BurstSparks();
                        }

                        float u = Ease.InOutSine((installT - ArmSeatShare) / (1f - ArmSeatShare));
                        _armTip.position = Vector3.Lerp(deckSocket, restWorld, u);
                    }

                    if (installT >= 1f)
                    {
                        _deckPhase = DeckPhase.Fitted;
                        _deckFitted = true;
                        _deck.gameObject.SetActive(false);
                        _deck.localScale = _deckScale;
                        _armTip.localPosition = _armRest;
                        _save.SaveNow();
                    }

                    break;
            }
        }

        private void ShowOnTheTray()
        {
            _deck.gameObject.SetActive(true);
            _deck.SetPositionAndRotation(_tray.position, _tray.rotation);
        }

        private void BurstSparks()
        {
            if (_sparks == null)
            {
                return;
            }

            _sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _sparks.Play(true);
        }

        private static ParticleSystem CreatePortSparks(Transform socket, Material material)
        {
            var host = new GameObject("PortWeldSparks");
            host.transform.SetParent(socket, false);
            var particles = host.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = SparkInterval * SparkBursts;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = Color.white;
            main.gravityModifier = 0.25f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = SparkCountPerBurst * SparkBursts;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            var bursts = new ParticleSystem.Burst[SparkBursts];
            for (int i = 0; i < bursts.Length; i++)
            {
                bursts[i] = new ParticleSystem.Burst(SparkInterval * i, (short)SparkCountPerBurst);
            }

            emission.SetBursts(bursts);

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 36f;
            shape.radius = 0.02f;
            shape.rotation = new Vector3(-25f, 0f, 0f);

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = fade;

            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            var particleRenderer = host.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.velocityScale = 0.06f;
            particleRenderer.lengthScale = 1f;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            particleRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            particleRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return particles;
        }

        /// <summary>
        /// Before the ring, the first time the player tries to tow (Tether pressed), 07 says once where the tether can
        /// be found again (text audit 0.5.5 §3.4: no hint for a problem the player doesn't have yet).
        /// </summary>
        private void HintTheTether()
        {
            if (_tetherHinted || _abilities.Has(RoverAbility.Tether) || _tether.AsleepPresses == 0)
            {
                return;
            }

            _tetherHinted = true;
            _events.Publish(new TickerLine(ProgressionKeys.NotYetTether));
        }

        /// <summary>A save from before the port that already used the tether keeps it (first-hour v2 §3).</summary>
        private bool UsedTheTether()
        {
            if (_abilities.Has(RoverAbility.CargoCradle))
            {
                return true;
            }

            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                if (_relics.Relics[i].State == RelicState.Displayed)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
