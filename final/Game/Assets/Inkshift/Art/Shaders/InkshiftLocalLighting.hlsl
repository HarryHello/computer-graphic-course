#ifndef INKSHIFT_LOCAL_LIGHTING_INCLUDED
#define INKSHIFT_LOCAL_LIGHTING_INCLUDED

void InkshiftLocalLighting(float3 positionWS, float2 screenUV, float3 normal, float3 view,
    float glossPower, out float3 diffuse, out float3 specular)
{
    diffuse = 0;
    specular = 0;

#if defined(_ADDITIONAL_LIGHTS)
    InputData inputData = (InputData)0;
    inputData.positionWS = positionWS;
    inputData.normalizedScreenSpaceUV = screenUV;

    LIGHT_LOOP_BEGIN(GetAdditionalLightsCount())
        Light light = GetAdditionalLight(lightIndex, positionWS, half4(1, 1, 1, 1));
        float3 radiance = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        float incidence = saturate(dot(normal, light.direction));
        float gloss = pow(saturate(dot(normal, SafeNormalize(light.direction + view))), glossPower);
        diffuse += radiance * incidence;
        specular += radiance * gloss * smoothstep(0.0, 0.12, incidence);
    LIGHT_LOOP_END
#endif
}

#endif
