using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Immutable specification of one R1 library material, linking its fixed Blender slot name, Unity material asset
    /// name, texture prefix, one-line intended use description, and physically-based surface parameters.
    /// </summary>
    public readonly struct R1MaterialSpec
    {
        public readonly R1MaterialSlot Slot;
        public readonly string SlotName;
        public readonly string MaterialAssetName;
        public readonly string TexturePrefix;
        public readonly string IntendedUse;
        public readonly Color BaseColor;
        public readonly float BumpScale;
        public readonly float MetallicScale;
        public readonly float SmoothnessMin;
        public readonly float SmoothnessMax;
        public readonly Color EdgeWearColor;
        public readonly float EdgeWearAmount;
        public readonly float EdgeWearMetallic;
        public readonly float EdgeWearSmoothness;
        public readonly float DustAmount;
        public readonly bool HasEmission;
        public readonly Color EmissionColor;
        public readonly float EmissionStrength;

        public R1MaterialSpec(
            R1MaterialSlot slot,
            string slotName,
            string materialAssetName,
            string texturePrefix,
            string intendedUse,
            Color baseColor,
            float bumpScale,
            float metallicScale,
            float smoothnessMin,
            float smoothnessMax,
            Color edgeWearColor,
            float edgeWearAmount,
            float edgeWearMetallic,
            float edgeWearSmoothness,
            float dustAmount,
            bool hasEmission,
            Color emissionColor,
            float emissionStrength)
        {
            Slot = slot;
            SlotName = slotName;
            MaterialAssetName = materialAssetName;
            TexturePrefix = texturePrefix;
            IntendedUse = intendedUse;
            BaseColor = baseColor;
            BumpScale = bumpScale;
            MetallicScale = metallicScale;
            SmoothnessMin = smoothnessMin;
            SmoothnessMax = smoothnessMax;
            EdgeWearColor = edgeWearColor;
            EdgeWearAmount = edgeWearAmount;
            EdgeWearMetallic = edgeWearMetallic;
            EdgeWearSmoothness = edgeWearSmoothness;
            DustAmount = dustAmount;
            HasEmission = hasEmission;
            EmissionColor = emissionColor;
            EmissionStrength = emissionStrength;
        }
    }
}
