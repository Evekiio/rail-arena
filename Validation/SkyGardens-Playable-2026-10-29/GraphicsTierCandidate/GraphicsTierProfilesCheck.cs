using System;
using RailArena;

internal static class GraphicsTierProfilesCheck
{
    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Main()
    {
        Expect(GraphicsTierProfiles.Normalize(-1) == GraphicsTier.High, "Negative value must fall back to High");
        Expect(GraphicsTierProfiles.Normalize(3) == GraphicsTier.High, "Unknown value must fall back to High");
        Expect(GraphicsTierProfiles.Next(GraphicsTier.High) == GraphicsTier.Balanced, "High must cycle to Balanced");
        Expect(GraphicsTierProfiles.Next(GraphicsTier.Balanced) == GraphicsTier.Performance, "Balanced must cycle to Performance");
        Expect(GraphicsTierProfiles.Next(GraphicsTier.Performance) == GraphicsTier.High, "Performance must wrap to High");
        var high = GraphicsTierProfiles.Get(GraphicsTier.High);
        var balanced = GraphicsTierProfiles.Get(GraphicsTier.Balanced);
        var performance = GraphicsTierProfiles.Get(GraphicsTier.Performance);
        Expect(high.RenderScale == 1f && high.ShadowDistance == 50f && high.ShadowCascades == 4 && high.MainShadowResolution == 2048 && high.LodBias == 2f,
            "High must retain the existing PC asset defaults");
        Expect(high.RenderScale > balanced.RenderScale && balanced.RenderScale > performance.RenderScale, "Render scales must decrease");
        Expect(high.ShadowDistance > balanced.ShadowDistance && balanced.ShadowDistance > performance.ShadowDistance, "Shadow distances must decrease");
        Expect(high.LodBias > balanced.LodBias && balanced.LodBias > performance.LodBias, "LOD bias must decrease");
        Expect(balanced.ShadowCascades == 4 && performance.ShadowCascades == 2, "Cascade counts differ from candidate profile");
        Expect(balanced.MainShadowResolution == 2048 && performance.MainShadowResolution == 1024, "Shadow resolutions differ from candidate profile");
        Console.WriteLine("GRAPHICS_TIER_PROFILE_CHECKS_PASSED");
    }
}
