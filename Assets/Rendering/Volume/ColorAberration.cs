using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("InPro/ColorAberration", typeof(UniversalRenderPipeline))]
public sealed partial class ColorAberration : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Strength of the color aberration.")]
    public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);
    
    public bool IsActive() => intensity.value > 0f;

    /// <inheritdoc/>
    public bool IsTileCompatible() => false;
}