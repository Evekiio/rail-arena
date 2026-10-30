using UnityEngine;

namespace RailArena
{
    [CreateAssetMenu(menuName = "Rail Arena/Cel art profile")]
    public sealed class CelArtProfile : ScriptableObject
    {
        public Shader SurfaceShader;
        [Range(0, 1)] public float LowerBand = .25f;
        [Range(0, 1)] public float UpperBand = .65f;
        [Range(.001f, .1f)] public float Softness = .03f;
        public Color ShadowPaletteSrgb = new(.65f, .715f, .79f);
        public Color MidPaletteSrgb = new(.865f, .9f, .925f);
        [Range(0, .5f)] public float MatteHighlight = .035f;
        [Range(0, .5f)] public float EnamelHighlight = .16f;

        public void Configure(Material target, Material source)
        {
            target.shader = SurfaceShader;
            target.SetFloat("_BandLow", LowerBand);
            target.SetFloat("_BandHigh", Mathf.Max(LowerBand + Softness, UpperBand));
            target.SetFloat("_BandSoftness", Softness);
            target.SetColor("_ShadowTint", ShadowPaletteSrgb.linear);
            target.SetColor("_MidTint", MidPaletteSrgb.linear);
            target.SetFloat("_HighlightStrength", source.GetFloat("_Metallic") > .2f ? EnamelHighlight : MatteHighlight);
            if (!source.IsKeywordEnabled("_EMISSION")) target.SetColor("_EmissionColor", Color.black);
        }
    }
}
