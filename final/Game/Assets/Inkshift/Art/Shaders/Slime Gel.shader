Shader "Inkshift/Slime Gel"
{
    Properties
    {
        _Tint("Gel tint", Color) = (1, 0.46, 0.19, 1)
        _Pattern("Interior ribbon", 2D) = "white" {}
        _PatternWeight("Ribbon visibility", Range(0, 1)) = 0
        _Refraction("Refraction strength", Range(0, 0.045)) = 0.024
        _PigmentDensity("Pigment density", Range(0, 1)) = 0
        _RoomReflection("Room reflection", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "Translucent gel"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "InkshiftLocalLighting.hlsl"

            TEXTURE2D(_Pattern);
            SAMPLER(sampler_Pattern);
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            float _PatternWeight;
            float _Refraction;
            float _PigmentDensity;
            float _RoomReflection;
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
                float fresnel = pow(1.0 - facing, 2.6);
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float2 bend = TransformWorldToViewDir(n).xy * _Refraction * (0.53 + 0.47 * facing);
                // Add only the refracted difference; replacing the framebuffer with the
                // opaque texture would erase wet paint already drawn behind the character.
                float3 bent = SampleSceneColor(saturate(screenUV - bend));
                float3 straight = SampleSceneColor(screenUV);
                float pattern = lerp(1.0, 0.83 + 0.17 * SAMPLE_TEXTURE2D(_Pattern, sampler_Pattern, input.uv).r,
                    _PatternWeight);

                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float softLight = saturate(dot(n, sun.direction) * 0.5 + 0.5);
                float light = 0.70 + 0.30 * softLight * sun.shadowAttenuation;
                float3 body = _Tint.rgb * light * pattern + (bent - straight) * 0.24;
                float highlight = pow(saturate(dot(n, SafeNormalize(sun.direction + view))), 98.0) * sun.shadowAttenuation;
                float sheen = pow(saturate(dot(n, SafeNormalize(float3(-0.44, 0.8, 0.40) + view))), 21.0);
                float3 rim = lerp(_Tint.rgb, float3(0.90, 1.0, 1.0), 0.42);
                float3 diffuse, specular;
                InkshiftLocalLighting(input.positionWS, screenUV, n, view, 78.0, diffuse, specular);
                float3 color = body + rim * fresnel * 0.28 + sun.color * highlight * 0.70 + sheen * 0.12;
                if (_RoomReflection > 0.0)
                {
                    float3 environment = GlossyEnvironmentReflection(reflect(-view, n), input.positionWS, 0.17, 1.0, screenUV);
                    color += environment * _RoomReflection * (0.12 + 0.46 * fresnel);
                }
                color += _Tint.rgb * diffuse * 0.17 + specular * 0.54;
                float opacity = saturate(lerp(0.50, 0.68, _PigmentDensity) + 0.13 * fresnel);
                return half4(max(color, 0.0), opacity);
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
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            float4 ShadowVertex(float3 positionOS : POSITION, float3 normalOS : NORMAL) : SV_POSITION
            {
                float3 position = TransformObjectToWorld(positionOS);
                float3 normal = TransformObjectToWorldNormal(normalOS);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirection = normalize(_LightPosition - position);
#else
                float3 lightDirection = _LightDirection;
#endif
                return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(position, normal, lightDirection)));
            }

            half4 ShadowFragment() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
