using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The worn bay outline on the apron World laid out (BuildApronMarkings/BayGlowOutline): it warms to Art's faint
    /// amber glow while build mode is open and cools again when it closes, so the player sees where a base belongs.
    /// Writes only <c>_BuildModeGlow</c> into the renderer's property block, keeping whatever else is in it.
    /// </summary>
    public sealed class ApronGlow
    {
        private static readonly int GlowId = Shader.PropertyToID("_BuildModeGlow");

        private readonly Renderer _outline;
        private readonly float _fadeSeconds;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private float _level;
        private float _shown = -1f;

        /// <param name="outline">The apron's bay outline renderer.</param>
        /// <param name="fadeSeconds">Seconds the glow takes to warm fully or cool off.</param>
        public ApronGlow(Renderer outline, float fadeSeconds)
        {
            _outline = outline != null ? outline : throw new ArgumentNullException(nameof(outline));
            _fadeSeconds = Mathf.Max(fadeSeconds, 0.01f);
            Apply();
        }

        /// <summary>The glow as written, 0 (unlit) .. 1 (Art's full build-mode glow).</summary>
        public float Glow => Ease.InOutSine(_level);

        /// <summary>Eases toward lit while <paramref name="buildModeOpen"/>, toward unlit otherwise.</summary>
        public void Step(bool buildModeOpen, float deltaTime)
        {
            _level = Mathf.MoveTowards(_level, buildModeOpen ? 1f : 0f, deltaTime / _fadeSeconds);
            Apply();
        }

        private void Apply()
        {
            float glow = Glow;
            if (Mathf.Approximately(glow, _shown))
            {
                return;
            }

            _shown = glow;
            _outline.GetPropertyBlock(_block);
            _block.SetFloat(GlowId, glow);
            _outline.SetPropertyBlock(_block);
        }
    }
}
