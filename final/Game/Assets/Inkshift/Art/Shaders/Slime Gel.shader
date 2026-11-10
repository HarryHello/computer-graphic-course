Shader "Inkshift/Slime Gel"
{
    Properties
    {
        _Tint("Gel tint", Color) = (1, 0.46, 0.19, 1)
        _Pattern("Interior ribbon", 2D) = "white" {}
        _PatternWeight("Ribbon visibility", Range(0, 1)) = 0
        _Refraction("Refraction strength", Range(0, 0.045)) = 0.024
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "Translucent gel"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_Pattern);
            SAMPLER(sampler_Pattern);
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            float _PatternWeight;
            float _Refraction;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float facing = saturate(dot(n, view));
                float fresnel = pow(1.0 - facing, 3.0);
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float2 bend = TransformWorldToViewDir(n).xy * _Refraction * (0.53 + 0.47 * facing);
                float3 background = SampleSceneColor(saturate(screenUV - bend));
                float pigment = lerp(1.0, SAMPLE_TEXTURE2D(_Pattern, sampler_Pattern, input.uv).r, _PatternWeight);

                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float softLight = saturate(dot(n, sun.direction) * 0.5 + 0.5);
                float light = 0.73 + 0.27 * softLight * sun.shadowAttenuation;
                float3 body = lerp(background * lerp(float3(1, 1, 1), _Tint.rgb, 0.55),
                    _Tint.rgb * light, 0.35 + 0.20 * facing) * pigment;
                float highlight = pow(saturate(dot(n, SafeNormalize(sun.direction + view))), 98.0) * sun.shadowAttenuation;
                float sheen = pow(saturate(dot(n, SafeNormalize(float3(-0.44, 0.8, 0.40) + view))), 21.0);
                float3 rim = lerp(_Tint.rgb, float3(0.84, 1, 1), 0.43);
                return half4(lerp(body, rim, 0.58 * fresnel) + sun.color * highlight * 0.75 + sheen * 0.17, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;

            float4 ShadowVertex(float3 positionOS : POSITION, float3 normalOS : NORMAL) : SV_POSITION
            {
                float3 position = TransformObjectToWorld(positionOS);
                float3 normal = TransformObjectToWorldNormal(normalOS);
                return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(position, normal, _LightDirection)));
            }

            half4 ShadowFragment() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
