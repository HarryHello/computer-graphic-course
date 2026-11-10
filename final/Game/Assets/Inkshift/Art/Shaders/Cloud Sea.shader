Shader "Inkshift/Cloud Sea"
{
    Properties
    {
        _Noise("Cloud density noise", 3D) = "white" {}
        _Zenith("Zenith", Color) = (0.12, 0.40, 0.76, 1)
        _Horizon("Horizon", Color) = (0.56, 0.77, 0.96, 1)
        _LowerBlue("Blue under the clouds", Color) = (0.24, 0.53, 0.80, 1)
        _CloudLight("Sunlit cloud", Color) = (1, 0.99, 0.95, 1)
        _CloudShade("Cloud shadow", Color) = (0.48, 0.64, 0.84, 1)
        _SunDirection("Direction to the sun", Vector) = (0.41, 0.74, -0.53, 0)
        _CloudHeights("Cloud base and top", Vector) = (-23, -6, 0, 0)
        _CloudShadowStrength("Cloud shadow strength", Range(0, 1)) = 0.67
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Cloud Sea"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SkyVertex
            #pragma fragment SkyFragment
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE3D(_Noise);
            SAMPLER(sampler_Noise);

            CBUFFER_START(UnityPerMaterial)
            float4 _Zenith;
            float4 _Horizon;
            float4 _LowerBlue;
            float4 _CloudLight;
            float4 _CloudShade;
            float4 _SunDirection;
            float4 _CloudHeights;
            float _CloudShadowStrength;
            CBUFFER_END

            struct SkyVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            SkyVaryings SkyVertex(float4 positionOS : POSITION)
            {
                SkyVaryings output;
                output.positionCS = TransformObjectToHClip(positionOS.xyz);
                output.direction = positionOS.xyz;
                return output;
            }

            float Noise(float3 samplePosition)
            {
                return SAMPLE_TEXTURE3D_LOD(_Noise, sampler_Noise, samplePosition, 0).r;
            }

            half4 SkyFragment(SkyVaryings input) : SV_Target
            {
                float3 ray = normalize(input.direction);
                float3 sun = normalize(_SunDirection.xyz);
                float3 sky = lerp(_Horizon.rgb, _Zenith.rgb, pow(saturate(ray.y), 0.43));
                sky = lerp(sky, _LowerBlue.rgb, saturate(-ray.y * 2.2));
                sky += float3(0.07, 0.052, 0.028) * pow(saturate(dot(ray, sun)), 15.0);

                if (ray.y > -0.025)
                    return half4(sky, 1.0);

                float3 camera = GetCameraPositionWS();
                float baseHeight = _CloudHeights.x;
                float topHeight = _CloudHeights.y;
                float nearCloud = max(0.0, (topHeight - camera.y) / ray.y);
                float farCloud = min(151.0, (baseHeight - camera.y) / ray.y);
                if (nearCloud >= farCloud)
                    return half4(sky, 1.0);

                int sampleCount = (int)clamp(ceil((farCloud - nearCloud) / 2.55), 18.0, 32.0);
                float interval = (farCloud - nearCloud) / sampleCount;
                float jitter = frac(52.9829189 * frac(dot(input.positionCS.xy, float2(0.06711056, 0.00583715))));
                float distance = nearCloud + interval * lerp(0.39, 0.61, jitter);
                float3 wind = float3(_Time.y * 0.0014, 0.0, _Time.y * 0.0008);
                float3 frequency = float3(0.018, 0.025, 0.018);
                float3 nearPoint = camera + ray * nearCloud;
                float weather = Noise(float3(nearPoint.x * 0.005, 0.37, nearPoint.z * 0.005) + wind * 0.35);
                float threshold = lerp(0.58, 0.44, smoothstep(0.26, 0.74, weather));
                float transmittance = 1.0;
                float3 radiance = 0.0;

                [loop]
                for (int i = 0; i < sampleCount; i++)
                {
                    float3 samplePosition = camera + ray * distance;
                    float altitude = saturate((samplePosition.y - baseHeight) / (topHeight - baseHeight));
                    float envelope = smoothstep(0.0, 0.29, altitude) * (1.0 - smoothstep(0.61, 1.0, altitude));
                    float3 noisePoint = samplePosition * frequency + wind;
                    float body = Noise(noisePoint);
                    float density = smoothstep(threshold, threshold + 0.19, body - (1.0 - envelope) * 0.23);
                    if (density > 0.006)
                    {
                        float towardSun = Noise(noisePoint + sun * frequency * 4.6);
                        float light = saturate(0.43 + (body - towardSun) * 3.3 + altitude * 0.47);
                        float shadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(samplePosition));
                        float attenuation = (1.0 - shadow) * _CloudShadowStrength;
                        float3 cloud = lerp(_CloudShade.rgb, _CloudLight.rgb, light);
                        cloud = lerp(cloud, cloud * float3(0.32, 0.46, 0.65), attenuation);
                        float opacity = 1.0 - exp(-density * interval * 0.79);
                        radiance += transmittance * opacity * cloud;
                        transmittance *= 1.0 - opacity;
                        if (transmittance < 0.014)
                            break;
                    }
                    distance += interval;
                }

                float3 throughCloud = radiance + transmittance * sky;
                return half4(lerp(throughCloud, sky, smoothstep(49.0, 143.0, nearCloud)), 1.0);
            }
            ENDHLSL
        }
    }
}
