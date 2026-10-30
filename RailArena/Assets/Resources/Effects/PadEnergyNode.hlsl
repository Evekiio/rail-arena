#include "ShaderApiReflectionSupport.hlsl"

/// <funchints>
///     <sg:ProviderKey>RailArena.PadEnergy.v1</sg:ProviderKey>
///     <sg:SearchCategory>Rail Arena/Traversal</sg:SearchCategory>
///     <sg:SearchTerms>jump, pad, energy, pulse</sg:SearchTerms>
///     <sg:DisplayName>Jump Pad Energy</sg:DisplayName>
/// </funchints>
/// <paramhints name="tint">
///     <sg:DisplayName>Energy Tint</sg:DisplayName>
///     <sg:Color />
///     <sg:Default>0.06,0.65,0.61</sg:Default>
/// </paramhints>
UNITY_EXPORT_REFLECTION float3 RailArenaPadEnergy(float2 localPosition, float clock, float burst, float calm, float charge, float3 tint)
{
    float radius = length(localPosition * 2.0);
    float rim = pow(saturate(1.0 - abs(radius - .84) / .12), 2.0);
    float pulse = pow(saturate(.5 + .5 * sin(radius * 25.0 - clock * 5.0)), 5.0);
    float field = .2 + pulse * .8 + rim * .9 + charge * .25;
    field += burst * (.3 + .65 * pow(saturate(.5 + .5 * sin(radius * 18.0 - clock * 8.0)), 3.0));
    field *= lerp(1.0, .55, calm);
    return tint * min(field, 2.5);
}
