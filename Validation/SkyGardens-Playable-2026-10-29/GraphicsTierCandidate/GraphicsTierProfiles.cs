using System;

namespace RailArena
{
    public enum GraphicsTier { High = 0, Balanced = 1, Performance = 2 }

    /// <summary>Candidate PC settings; visual and performance validation remain required.</summary>
    public readonly struct GraphicsTierProfile
    {
        public readonly float RenderScale;
        public readonly float ShadowDistance;
        public readonly int ShadowCascades;
        public readonly int MainShadowResolution;
        public readonly float LodBias;

        public GraphicsTierProfile(float renderScale, float shadowDistance, int shadowCascades,
            int mainShadowResolution, float lodBias)
        {
            RenderScale = renderScale;
            ShadowDistance = shadowDistance;
            ShadowCascades = shadowCascades;
            MainShadowResolution = mainShadowResolution;
            LodBias = lodBias;
        }
    }

    public static class GraphicsTierProfiles
    {
        public static GraphicsTier Normalize(int value) => value >= 0 && value <= 2 ? (GraphicsTier)value : GraphicsTier.High;
        public static GraphicsTier Next(GraphicsTier current) => (GraphicsTier)(((int)Normalize((int)current) + 1) % 3);

        public static GraphicsTierProfile Get(GraphicsTier tier)
        {
            switch (Normalize((int)tier))
            {
                case GraphicsTier.Balanced: return new GraphicsTierProfile(.9f, 40f, 4, 2048, 1.5f);
                case GraphicsTier.Performance: return new GraphicsTierProfile(.75f, 30f, 2, 1024, 1f);
                default: return new GraphicsTierProfile(1f, 50f, 4, 2048, 2f);
            }
        }
    }
}
