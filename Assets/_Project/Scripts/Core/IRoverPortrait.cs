using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// A live render of 07 from a fixed side-three-quarter camera for the Tab "07" equipment page
    /// (docs/design/0.5.5-owner-feedback.md §3.5), registered by the Rover domain: fitted parts show as they really
    /// look. It renders only while active, so it costs nothing otherwise.
    /// </summary>
    public interface IRoverPortrait
    {
        /// <summary>The render target (transparent around 07), the same texture for the whole session.</summary>
        RenderTexture Texture { get; }

        /// <summary>Starts or stops rendering (the page opened or closed).</summary>
        void SetActive(bool active);

        /// <summary>
        /// Where <paramref name="socket"/> sits in <see cref="Texture"/> (0..1, origin bottom-left), from art's
        /// socket node on 07; false when the socket has no node or is out of the frame.
        /// </summary>
        bool TryGetSocketViewport(RoverSocket socket, out Vector2 uv);
    }
}
