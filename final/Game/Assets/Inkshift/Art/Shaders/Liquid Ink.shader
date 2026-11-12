Shader "Inkshift/Liquid Ink"
{
    Properties
    {
        _InkState("Simulated pigment and wetness", 2D) = "black" {}
        _Orange("Tangerine pigment", Color) = (1, 0.33, 0.08, 1)
        _Aqua("Aqua pigment", Color) = (0.01, 0.78, 0.67, 1)
        _RoomReflection("Room reflection", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest+15" "RenderType"="TransparentCutout" }
        Pass
        {
            Name "Wet surface"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "InkshiftLocalLighting.hlsl"

            TEXTURE2D(_InkState);
            SAMPLER(sampler_InkState);
            CBUFFER_START(UnityPerMaterial)
            float4 _InkState_TexelSize;
            float4 _Orange;
            float4 _Aqua;
            float _RoomReflection;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half fog : TEXCOORD2;
            };

            float Mass(float2 uv)
            {
                return dot(SAMPLE_TEXTURE2D_LOD(_InkState, sampler_InkState, uv, 0).rg, float2(1.0, 1.0));
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 up = TransformObjectToWorldNormal(float3(0, 1, 0));
                output.positionWS = TransformObjectToWorld(input.positionOS) + up * min(Mass(input.uv), 1.0) * 0.042;
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 state = SAMPLE_TEXTURE2D(_InkState, sampler_InkState, input.uv).rgb;
                float height = state.x + state.y;
                clip(height - 0.070);

                float2 texel = abs(_InkState_TexelSize.xy);
                float dx = Mass(input.uv + float2(texel.x, 0)) - Mass(input.uv - float2(texel.x, 0));
                float dz = Mass(input.uv + float2(0, texel.y)) - Mass(input.uv - float2(0, texel.y));
                float phase = dot(input.positionWS.xz, float2(8.7, 3.2)) + _Time.y * 2.1;
                float crossWave = dot(input.positionWS.xz, float2(-5.1, 12.0)) - _Time.y * 1.35;
                float wet = 0.45 + 0.55 * state.z;
                float2 ripples = wet * float2(cos(phase) * 0.041 + sin(crossWave) * 0.024,
                    cos(phase) * 0.021 + cos(crossWave) * 0.043);
                float3 up = TransformObjectToWorldDir(float3(0, 1, 0));
                float3 n = normalize(up -
                    TransformObjectToWorldDir(float3(1, 0, 0)) * (dx * 2.45 + ripples.x) -
                    TransformObjectToWorldDir(float3(0, 0, 1)) * (dz * 2.45 + ripples.y));
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float indoors = saturate(_RoomReflection * 2.0);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float diffuse = saturate(dot(n, sun.direction)) * sun.shadowAttenuation;
                float3 halfway = SafeNormalize(sun.direction + view);
                float gloss = pow(saturate(dot(n, halfway)), lerp(28.0, 82.0, wet)) * sun.shadowAttenuation;
                float3 reflection = reflect(-view, n);
                float skyGlint = pow(saturate(dot(reflection, normalize(float3(-0.28, 0.83, 0.49)))), 12.0);
                float rim = pow(1.0 - saturate(dot(n, view)), 3.5);
                float3 pigment = lerp(_Aqua.rgb, _Orange.rgb, state.x / height);
                float pool = smoothstep(0.07, 0.28, height);
                float meniscus = smoothstep(0.079, 0.19, height) * (1.0 - smoothstep(0.24, 0.55, height));
                float edgeLight = 0.35 + 0.65 * saturate(dot(n, normalize(float3(-0.61, 0.65, -0.38))));
                float3 color = pigment * (0.54 + 0.53 * diffuse) * lerp(0.66, 1.0, pool);
                color += sun.color * gloss * lerp(0.32, 1.05, wet);
                color += (skyGlint * (0.09 + 0.13 * wet) + rim * 0.22) * float3(0.69, 0.84, 1.0) * lerp(1.0, 0.18, indoors);
                color += meniscus * edgeLight * wet * lerp(float3(0.38, 0.45, 0.51), pigment * 0.26 + 0.025, indoors);
                float3 localDiffuse, localGloss;
                float glossPower = lerp(24.0, 70.0, wet) * lerp(1.0, 3.4, indoors);
                InkshiftLocalLighting(input.positionWS, screenUV, n, view, glossPower, localDiffuse, localGloss);
                color += pigment * localDiffuse * lerp(0.12, 0.27, pool);
                color += localGloss * lerp(0.34, 0.85, wet) * lerp(1.0, 1.65, indoors);
                if (_RoomReflection > 0.0)
                {
                    float roughness = lerp(0.25, 0.11, wet);
                    float3 environment = GlossyEnvironmentReflection(reflection, input.positionWS, roughness, 1.0, screenUV);
                    color += environment * _RoomReflection * (0.18 + 0.72 * rim);
                }
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
