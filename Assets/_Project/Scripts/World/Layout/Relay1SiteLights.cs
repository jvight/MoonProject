using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// relay.1's repair site lit as it waits for its part (opening v3 5.1): the service cabinet's lamp strip glows warm
    /// over the spare (its real light is the scene build's point light at the cabinet's Light_Anchor), the mast's feed
    /// socket outline asks for the part, and the contact pad's lamp stays dark until the relay is relit. Art's props
    /// carry the emissive parts; their glow is set through property blocks, once.
    /// </summary>
    public sealed class Relay1SiteLights : MonoBehaviour, IGameSystem
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' relay.1 site.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The service cabinet's lamp strip (Art's InteriorLamp).")]
        [SerializeField] private Renderer _interiorLamp;

        [Tooltip("The feed socket's outline on the mast box (Art's FeedSocket_Outline).")]
        [SerializeField] private Renderer _socketOutline;

        [Tooltip("The contact pad's lamp (Art's PadLamp), dark until the relay is relit.")]
        [SerializeField] private Renderer _padLamp;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _interiorLamp == null || _socketOutline == null || _padLamp == null)
            {
                Debug.LogError($"{nameof(Relay1SiteLights)}: World Settings, the cabinet's lamp strip, the socket's " +
                    "outline and the pad's lamp must be assigned (the scene build wires them at relay.1).", this);
                enabled = false;
                return;
            }

            Relay1SiteSettings site = _settings.Landmarks.Relay1Site;
            var block = new MaterialPropertyBlock();
            Glow(block, _interiorLamp, site.LampColor * site.LampStripGlow);
            Glow(block, _socketOutline, site.OutlineColor * site.OutlineNeedsPart);
            Glow(block, _padLamp, Color.black);
        }

        // Emission is linear light: written as a vector, so it is not taken for a colour to convert.
        private static void Glow(MaterialPropertyBlock block, Renderer target, Color linear)
        {
            target.GetPropertyBlock(block);
            block.SetVector(EmissionColorId, new Vector4(linear.r, linear.g, linear.b, 1f));
            target.SetPropertyBlock(block);
        }
    }
}
