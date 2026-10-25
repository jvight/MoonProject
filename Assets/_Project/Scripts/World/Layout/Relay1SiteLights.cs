using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// relay.1's repair site through its repair (opening v3 5.1, the owner's "nothing happens when I fit the part").
    /// While it waits for its part, the service cabinet's lamp strip glows warm over the spare (its real light is the
    /// scene build's point light at the cabinet's Light_Anchor), the mast's feed socket outline asks for the part, and
    /// the contact pad's lamp, the relay lens and the dial lamp are dark. Then it steps through the power-up
    /// (<see cref="RelayPowerStage"/>, timed by Gameplay): at Latch the fitted unit makes its last push home and the
    /// outline turns from asking to holding; at Wake the relay lens and the dial lamp come up; at Align the dish
    /// turns to the Peak's Great Dish and settles; at Live the cabinet's strip and light go out and the pad's lamp
    /// comes on. A relit relay loaded from a save stands at its end at once. Art's props carry the emissive parts;
    /// their glow is set through property blocks. Allocation-free per frame, and idle once the power-up has settled.
    /// </summary>
    public sealed class Relay1SiteLights : MonoBehaviour, IGameSystem
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        // The relay this site belongs to (its world anchor's id).
        private const string RelayId = WorldAnchorIds.RelayPrefix + "1";

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' relay.1 site.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The service cabinet's lamp strip (Art's InteriorLamp).")]
        [SerializeField] private Renderer _interiorLamp;

        [Tooltip("The real light the cabinet's lamp casts inside it (at Art's Light_Anchor).")]
        [SerializeField] private Light _interiorLight;

        [Tooltip("The feed socket's outline on the mast box (Art's FeedSocket_Outline).")]
        [SerializeField] private Renderer _socketOutline;

        [Tooltip("The contact pad's lamp (Art's PadLamp), dark until the relay is live.")]
        [SerializeField] private Renderer _padLamp;

        [Tooltip("The mast's relay lens (Art's RelayLamp_Lens), dark until the relay wakes.")]
        [SerializeField] private Renderer _relayLens;

        [Tooltip("The mast's dial lamp (Art's DialLamp), dark until the relay wakes.")]
        [SerializeField] private Renderer _dialLamp;

        [Tooltip("The fitted feed unit in the socket (Gameplay shows it at the fit), seated home at rest.")]
        [SerializeField] private Transform _fitted;

        [Tooltip("Direction into the socket, in the fitted unit's parent's space (baked by the scene build).")]
        [SerializeField] private Vector3 _seatIn = Vector3.forward;

        [Tooltip("The dish's azimuth pivot on the mast (Art's DishAzimuth).")]
        [SerializeField] private Transform _dish;

        [Tooltip("Degrees the dish turns about the vertical from where it stands slewed to face the Peak's Great Dish " +
            "(baked by the scene build).")]
        [SerializeField] private float _dishTurn;

        private readonly RelayPowerUp _powerUp = new RelayPowerUp();
        private MaterialPropertyBlock _block;
        private IDisposable _stages;
        private Vector3 _seated;
        private Quaternion _dishRest;
        private bool _settled;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _interiorLamp == null || _interiorLight == null || _socketOutline == null
                || _padLamp == null || _relayLens == null || _dialLamp == null || _fitted == null || _dish == null
                || _seatIn == Vector3.zero)
            {
                Debug.LogError($"{nameof(Relay1SiteLights)}: World Settings, the cabinet's lamp strip and light, the " +
                    "socket's outline, the pad's lamp, the relay lens, the dial lamp, the fitted unit with its way in " +
                    "and the dish's pivot must be assigned (the scene build wires them at relay.1).", this);
                enabled = false;
                return;
            }

            _block = new MaterialPropertyBlock();

            // The dial's dark glass has no glow of its own: its emission is opened once, its colour set as it wakes.
            _dialLamp.GetPropertyBlock(_block);
            _block.SetFloat(EmissionStrengthId, 1f);
            _dialLamp.SetPropertyBlock(_block);
            _seated = _fitted.localPosition;
            _dishRest = _dish.rotation;
            Show();
            _settled = true;
            _stages = context.Events.Subscribe<RelayPowerStage>(OnStage);
        }

        /// <summary>The site shows the relay live.</summary>
        public bool Relit => _powerUp.Reached(RelayStage.Live);

        /// <summary>How far the dish has turned to the Peak, 0 (at rest) to 1 (facing it).</summary>
        public float DishAligned { get; private set; }

        private void OnStage(RelayPowerStage stage)
        {
            if (stage.RelayId == RelayId && _block != null)
            {
                _powerUp.OnStage(stage.Stage, stage.Restored);
                _settled = false;
            }
        }

        private void Update()
        {
            if (_settled)
            {
                return;
            }

            _powerUp.Step(Time.deltaTime);
            Show();
            Relay1SiteSettings site = _settings.Landmarks.Relay1Site;
            _settled = _powerUp.Reached(RelayStage.Live)
                && _powerUp.Progress(RelayStage.Latch, site.SeatSeconds) >= 1f
                && _powerUp.Progress(RelayStage.Wake, site.WakeSeconds) >= 1f
                && _powerUp.Progress(RelayStage.Align, site.AlignSeconds) >= 1f;
        }

        private void Show()
        {
            Relay1SiteSettings site = _settings.Landmarks.Relay1Site;
            float seat = RelayPowerUp.Settle(_powerUp.Progress(RelayStage.Latch, site.SeatSeconds), site.Overshoot,
                site.SettleFrom);
            _fitted.localPosition = _seated - _seatIn * (site.SeatDepth * (1f - seat));
            float held = _powerUp.Reached(RelayStage.Latch) ? Mathf.Clamp01(seat) : 0f;
            Glow(_socketOutline, site.OutlineColor * Mathf.Lerp(site.OutlineNeedsPart, site.OutlineFitted, held));

            float wake = SmoothMath.Smootherstep(0f, 1f, _powerUp.Progress(RelayStage.Wake, site.WakeSeconds));
            Glow(_relayLens, site.LampColor * (site.RelayLensGlow * wake));
            Glow(_dialLamp, site.LampColor * (site.DialLampGlow * wake));

            DishAligned = RelayPowerUp.Settle(_powerUp.Progress(RelayStage.Align, site.AlignSeconds), site.Overshoot,
                site.SettleFrom);
            _dish.rotation = Quaternion.AngleAxis(_dishTurn * DishAligned, Vector3.up) * _dishRest;

            bool live = _powerUp.Reached(RelayStage.Live);
            _interiorLight.enabled = !live;
            Glow(_interiorLamp, live ? Color.black : site.LampColor * site.LampStripGlow);
            Glow(_padLamp, live ? site.LampColor * site.PadLampGlow : Color.black);
        }

        // Emission is linear light: written as a vector, so it is not taken for a colour to convert.
        private void Glow(Renderer target, Color linear)
        {
            target.GetPropertyBlock(_block);
            _block.SetVector(EmissionColorId, new Vector4(linear.r, linear.g, linear.b, 1f));
            target.SetPropertyBlock(_block);
        }

        private void OnDestroy()
        {
            _stages?.Dispose();
            _stages = null;
        }
    }
}
