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
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
        Pass
        {
            Name "Wet surface"
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
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 edge : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float2 edge : TEXCOORD3;
                half fog : TEXCOORD4;
            };

            float3 State(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_InkState, sampler_InkState, uv, 0).rgb;
            }

            float Mass(float2 uv)
            {
                return dot(State(uv).rg, float2(1.0, 1.0));
            }

            // Edge skirts read the very same field as the top, so their pigment and lifetime stay joined.
            float DripReach(float3 positionWS, float height, float wetness)
            {
                float along = dot(positionWS.xz, float2(5.9, 8.3));
                float shape = 0.5 + 0.23 * sin(along) + 0.17 * sin(along * 0.43 + 1.7)
                    + 0.10 * sin(along * 1.77 + 0.9);
                float growth = lerp(1.0, 0.28, saturate(wetness));
                return saturate((height - 0.045) * 1.45) * lerp(0.28, 1.0, saturate(shape)) * growth;
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 normal = TransformObjectToWorldNormal(input.normalOS);
                float3 basePosition = TransformObjectToWorld(input.positionOS);
                float3 state = State(input.uv);
                float height = state.x + state.y;
                float raised = min(height, 1.0);
                float edgeDepth = saturate(input.edge.x * 4.0);
                float extrusion = raised * lerp(0.034, 0.015 * edgeDepth, input.edge.y);
                float3 join = TransformObjectToWorldDir(float3(0, 1, 0)) *
                    (input.edge.y * raised * 0.034 * (1.0 - edgeDepth));
                output.positionWS = basePosition + normal * extrusion + join;
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = normal;
                output.uv = input.uv;
                output.edge = input.edge;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 state = State(input.uv);
                float height = state.x + state.y;
                clip(height - 0.008);
                float edgeCoverage = 1.0;
                if (input.edge.y > 0.5)
                {
                    float reach = DripReach(input.positionWS, height, state.z);
                    edgeCoverage = 1.0 - smoothstep(reach - 0.035, reach + 0.035, input.edge.x);
                    clip(edgeCoverage - 0.005);
                }

                float2 texel = abs(_InkState_TexelSize.xy);
                float dx = Mass(input.uv + float2(texel.x, 0)) - Mass(input.uv - float2(texel.x, 0));
                float dz = Mass(input.uv + float2(0, texel.y)) - Mass(input.uv - float2(0, texel.y));
                float phase = dot(input.positionWS.xz, float2(8.7, 3.2)) + _Time.y * 2.1;
                float crossWave = dot(input.positionWS.xz, float2(-5.1, 12.0)) - _Time.y * 1.35;
                float wet = 0.45 + 0.55 * state.z;
                float2 ripples = wet * float2(cos(phase) * 0.030 + sin(crossWave) * 0.018,
                    cos(phase) * 0.016 + cos(crossWave) * 0.032);
                float3 surfaceNormal = normalize(input.normalWS);
                float3 n = normalize(surfaceNormal -
                    TransformObjectToWorldDir(float3(1, 0, 0)) * (dx * 2.45 + ripples.x) -
                    TransformObjectToWorldDir(float3(0, 0, 1)) * (dz * 2.45 + ripples.y));
                if (input.edge.y > 0.5)
                    n = normalize(surfaceNormal + TransformObjectToWorldDir(float3(0, 1, 0)) * ripples.y);
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
                float3 pigment = lerp(_Aqua.rgb, _Orange.rgb, state.x / max(height, 0.0001));
                float pool = smoothstep(0.04, 0.40, height);
                float meniscus = smoothstep(0.035, 0.13, height) * (1.0 - smoothstep(0.24, 0.55, height));
                float edgeLight = 0.35 + 0.65 * saturate(dot(n, normalize(float3(-0.61, 0.65, -0.38))));
                float3 color = pigment * (0.62 + 0.45 * diffuse) * lerp(0.8, 1.0, pool);
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
                float opacity = smoothstep(0.008, 0.16, height) * lerp(0.28, 0.64, smoothstep(0.04, 1.0, height));
                return half4(MixFog(color, input.fog), opacity * edgeCoverage);
            }
            ENDHLSL
        }
    }
}
