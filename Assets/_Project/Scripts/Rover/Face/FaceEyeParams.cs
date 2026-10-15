using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Resolved per-eye CRT shape and placement parameters for either the primary phosphor layer or the trailing ghost.
    /// </summary>
    public readonly struct FaceEyeParams
    {
        public const float PrototypeSizeScale = 1.18f;
        public const float MinRenderedHeight = 0.006f;

        public FaceEyeParams(
            float centerX,
            float centerY,
            float width,
            float height,
            float open,
            float tilt,
            float cut,
            float squint,
            float arc,
            float ring,
            float closed,
            float lookX,
            float lookY,
            float scale)
        {
            CenterX = centerX;
            CenterY = centerY;
            Width = width;
            Height = height;
            Open = open;
            Tilt = tilt;
            Cut = cut;
            Squint = squint;
            Arc = arc;
            Ring = ring;
            Closed = closed;
            LookX = lookX;
            LookY = lookY;
            Scale = scale;
        }

        /// <summary>Horizontal eye center in normalized [0..1] screen width.</summary>
        public float CenterX { get; }

        /// <summary>Vertical eye center in normalized [0..1] top-down screen height.</summary>
        public float CenterY { get; }

        /// <summary>Base eye width parameter (prototype <c>w</c>).</summary>
        public float Width { get; }

        /// <summary>Base eye height parameter (prototype <c>h</c>).</summary>
        public float Height { get; }

        /// <summary>Effective vertical eye openness after feeling open, blink, and boot opening.</summary>
        public float Open { get; }

        /// <summary>Top eyelid tilt angle in radians (prototype <c>tilt</c>).</summary>
        public float Tilt { get; }

        /// <summary>Top eyelid cut fraction from the top of the eye (prototype <c>cut</c>).</summary>
        public float Cut { get; }

        /// <summary>Bottom eyelid squint fraction from the bottom of the eye (prototype <c>squint</c>).</summary>
        public float Squint { get; }

        /// <summary>Upward happy crescent arc weight in [0..1] (prototype <c>arc</c>).</summary>
        public float Arc { get; }

        /// <summary>Startled hollow ellipse ring weight in [0..1] (prototype <c>ring</c>).</summary>
        public float Ring { get; }

        /// <summary>Asleep downward closed crescent weight in [0..1] (prototype <c>closed</c>).</summary>
        public float Closed { get; }

        /// <summary>Combined horizontal look offset (feeling + external gaze + saccade).</summary>
        public float LookX { get; }

        /// <summary>Combined vertical look offset in top-down space (feeling + external gaze + saccade).</summary>
        public float LookY { get; }

        /// <summary>Per-eye scale multiplier (1.0 when symmetric; 1.12 left / 0.90 right when curious).</summary>
        public float Scale { get; }

        /// <summary>Rendered width in screen-height units (<c>w * scale * 1.18</c>).</summary>
        public float RenderedWidth => Width * Scale * PrototypeSizeScale;

        /// <summary>Rendered height in screen-height units (<c>max(0.006, h * open * scale * 1.18)</c>).</summary>
        public float RenderedHeight => Math.Max(MinRenderedHeight, Height * Open * Scale * PrototypeSizeScale);

        /// <summary>Packs <c>(CenterX, CenterY, RenderedWidth, RenderedHeight)</c> for the CRT shader.</summary>
        public Vector4 ToRectVector()
        {
            return new Vector4(CenterX, CenterY, RenderedWidth, RenderedHeight);
        }

        /// <summary>Packs <c>(Tilt, Cut, Squint, Open)</c> for the CRT shader.</summary>
        public Vector4 ToShapeVector()
        {
            return new Vector4(Tilt, Cut, Squint, Open);
        }

        /// <summary>Packs <c>(Arc, Ring, Closed, Scale)</c> for the CRT shader.</summary>
        public Vector4 ToModeVector()
        {
            return new Vector4(Arc, Ring, Closed, Scale);
        }

        /// <summary>Linearly interpolates all eye parameters between <paramref name="a"/> and <paramref name="b"/>.</summary>
        public static FaceEyeParams Lerp(FaceEyeParams a, FaceEyeParams b, float t)
        {
            float clamped = t < 0f ? 0f : (t > 1f ? 1f : t);
            return new FaceEyeParams(
                a.CenterX + (b.CenterX - a.CenterX) * clamped,
                a.CenterY + (b.CenterY - a.CenterY) * clamped,
                a.Width + (b.Width - a.Width) * clamped,
                a.Height + (b.Height - a.Height) * clamped,
                a.Open + (b.Open - a.Open) * clamped,
                a.Tilt + (b.Tilt - a.Tilt) * clamped,
                a.Cut + (b.Cut - a.Cut) * clamped,
                a.Squint + (b.Squint - a.Squint) * clamped,
                a.Arc + (b.Arc - a.Arc) * clamped,
                a.Ring + (b.Ring - a.Ring) * clamped,
                a.Closed + (b.Closed - a.Closed) * clamped,
                a.LookX + (b.LookX - a.LookX) * clamped,
                a.LookY + (b.LookY - a.LookY) * clamped,
                a.Scale + (b.Scale - a.Scale) * clamped);
        }
    }
}
