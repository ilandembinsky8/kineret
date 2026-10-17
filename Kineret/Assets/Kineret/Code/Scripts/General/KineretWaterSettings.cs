using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mirrors StreamingAssets/KineretWater.json. Flat, because JsonUtility has no dictionary support.
///
/// The look is held in named presets so the four candidates can be compared without a rebuild:
/// ActivePreset picks one, and its fields overwrite the ones on this object.
/// </summary>
[Serializable]
public class KineretWaterSettings
{
    [Header("Placement")]
    public float SurfaceY = 117.61f;
    public float HeightAboveSurface = 1f;

    public float XMin = 82736.70f;
    public float XMax = 96295.70f;
    public float ZMin = 54031.26f;
    public float ZMax = 75756.59f;

    /// <summary>Metres that a full-white pixel in the shore map's red channel stands for.</summary>
    public float MaxShoreDistance = 600f;

    public bool StartVisible = true;

    [Header("Look")]
    public string ActivePreset = "Waves";
    public List<KineretWaterPreset> Presets = new List<KineretWaterPreset>();

    public string DeepColorHex = "#04202B";
    public string ShallowColorHex = "#124D55";
    public float ShallowDistance = 250f;
    public float EdgeFade = 40f;
    public float Opacity = 1f;
    public float Smoothness = 0.92f;
    public float SunGlint = 1f;
    public float SkyReflection = 0.35f;
    public float WaveStrength = 0.25f;
    public float WaveScale = 45f;
    public float WaveSpeed = 0.6f;
    public float WaveFadeDistance = 4000f;
    public float WaveContrast = 0.6f;

    public void ResolvePreset() { ResolvePreset(ActivePreset); }

    public void ResolvePreset(string presetName)
    {
        if (string.IsNullOrEmpty(presetName) || Presets == null)
            return;

        foreach (KineretWaterPreset preset in Presets)
        {
            if (preset == null || preset.Name != presetName)
                continue;

            ActivePreset = presetName;

            DeepColorHex = preset.DeepColorHex;
            ShallowColorHex = preset.ShallowColorHex;
            ShallowDistance = preset.ShallowDistance;
            EdgeFade = preset.EdgeFade;
            Opacity = preset.Opacity;
            Smoothness = preset.Smoothness;
            SunGlint = preset.SunGlint;
            SkyReflection = preset.SkyReflection;
            WaveStrength = preset.WaveStrength;
            WaveScale = preset.WaveScale;
            WaveSpeed = preset.WaveSpeed;
            WaveFadeDistance = preset.WaveFadeDistance;
            WaveContrast = preset.WaveContrast;
            return;
        }

        Debug.LogWarning($"KineretWaterSettings: no preset named '{presetName}', keeping the current values.");
    }

    public Color GetDeepColour() { return Parse(DeepColorHex, new Color(0.015f, 0.125f, 0.169f)); }
    public Color GetShallowColour() { return Parse(ShallowColorHex, new Color(0.07f, 0.30f, 0.33f)); }

    private static Color Parse(string hex, Color fallback)
    {
        Color parsed;
        if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out parsed))
            return parsed;

        Debug.LogWarning($"KineretWaterSettings: could not parse colour '{hex}'.");
        return fallback;
    }
}

[Serializable]
public class KineretWaterPreset
{
    public string Name;

    public string DeepColorHex = "#04202B";
    public string ShallowColorHex = "#124D55";
    public float ShallowDistance = 250f;
    public float EdgeFade = 40f;
    public float Opacity = 1f;
    public float Smoothness = 0.92f;
    public float SunGlint = 1f;
    public float SkyReflection = 0.35f;
    public float WaveStrength = 0.25f;
    public float WaveScale = 120f;
    public float WaveSpeed = 0.6f;
    public float WaveFadeDistance = 4000f;
    public float WaveContrast = 0.6f;
}
