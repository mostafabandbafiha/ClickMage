using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("Custom/LUT Blender")]
public class LUTBlenderComponent : VolumeComponent, IPostProcessComponent
{
    public TextureParameter lut1 = new TextureParameter(null);
    public TextureParameter lut2 = new TextureParameter(null);

    [Range(0f, 1f)]
    public ClampedFloatParameter blend = new ClampedFloatParameter(0f, 0f, 1f);

    [Range(0f, 1f)]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 1f);

    //public bool IsActive() => lut1.value != null && lut2.value != null;
    public bool IsTileCompatible() => false;
    public bool IsActive()
    {
        // Active if at least lut1 is assigned
        return lut1.value != null;
    }
}
