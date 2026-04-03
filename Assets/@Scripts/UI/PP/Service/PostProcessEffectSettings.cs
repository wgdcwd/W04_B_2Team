using System;
using UnityEngine;

[Serializable]
public struct VignetteEffectSettings
{
    public Color startColor;
    public Color endColor;
    [Range(0f, 1f)] public float startIntensity;
    [Range(0f, 1f)] public float endIntensity;
    public float duration;
}

[Serializable]
public struct ChromaticAberrationEffectSettings
{
    [Range(0f, 1f)] public float startIntensity;
    [Range(0f, 1f)] public float endIntensity;
    public float duration;
}

[Serializable]
public struct LensDistortionEffectSettings
{
    [Range(-1f, 1f)] public float startIntensity;
    [Range(-1f, 1f)] public float endIntensity;
    public float xMultiplier;
    public float yMultiplier;
    public float duration;
}

[Serializable]
public struct FilmGrainEffectSettings
{
    [Range(0f, 1f)] public float startIntensity;
    [Range(0f, 1f)] public float endIntensity;
    [Range(0f, 1f)] public float response;
    public float duration;
}
