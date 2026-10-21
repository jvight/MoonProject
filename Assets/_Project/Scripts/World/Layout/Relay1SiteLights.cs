using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// relay.1's repair site lit for where the repair stands (opening v3 5.1). While it waits for its part, the service
    /// cabinet's lamp strip glows warm over the spare (its real light is the scene build's point light at the
    /// cabinet's Light_Anchor), the mast's feed socket outline asks for the part, and the contact pad's lamp and the
    /// mast's relay lens are dark. Once relit (<see cref="RelayRelit"/>, this session or a loaded one) the cabinet's
    /// strip and light go out, the outline glows brighter round the fitted unit, and the pad's lamp and the relay lens
    /// come on. Art's props carry the emissive parts; their glow is set through property blocks, only on a change.
    /// </summary>
    public sealed class Relay1SiteLights : MonoBehaviour, IGameSystem
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

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

        [Tooltip("The contact pad's lamp (Art's PadLamp), dark until the relay is relit.")]
        [SerializeField] private Renderer _padLamp;

        [Tooltip("The mast's relay lens (Art's RelayLamp_Lens), dark until the relay is relit.")]
        [SerializeField] private Renderer _relayLens;

        private MaterialPropertyBlock _block;
        private IDisposable _relit;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _interiorLamp == null || _interiorLight == null || _socketOutline == null
                || _padLamp == null || _relayLens == null)
            {
                Debug.LogError($"{nameof(Relay1SiteLights)}: World Settings, the cabinet's lamp strip and light, the " +
                    "socket's outline, the pad's lamp and the relay lens must be assigned (the scene build wires them " +
                    "at relay.1).", this);
                enabled = false;
                return;
            }

            _block = new MaterialPropertyBlock();
            Show(false);
            _relit = context.Events.Subscribe<RelayRelit>(OnRelit);
        }

        /// <summary>The site shows the relay relit.</summary>
        public bool Relit { get; private set; }

        private void OnRelit(RelayRelit relit)
        {
            if (relit.RelayId == RelayId && _block != null)
            {
                Show(true);
            }
        }

        private void Show(bool relit)
        {
            Relay1SiteSettings site = _settings.Landmarks.Relay1Site;
            Relit = relit;
            _interiorLight.enabled = !relit;
            Glow(_interiorLamp, relit ? Color.black : site.LampColor * site.LampStripGlow);
            Glow(_socketOutline, site.OutlineColor * (relit ? site.OutlineFitted : site.OutlineNeedsPart));
            Glow(_padLamp, relit ? site.LampColor * site.PadLampGlow : Color.black);
            Glow(_relayLens, relit ? site.LampColor * site.RelayLensGlow : Color.black);
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
            _relit?.Dispose();
            _relit = null;
        }
    }
}
