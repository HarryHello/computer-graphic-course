Shader "Hidden/Inkshift/Paint Flow"
{
    Properties
    {
        _MainTex("Previous field", 2D) = "black" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Fragment
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Surface; // Size in metres, downhill flow in UV units per second.
            float4 _Segment; // Start and end in surface UV space.
            float4 _Deposit; // Radius, pigment mass, orange fraction, impact.
            float4 _Ripple;  // Last impact UV, outward velocity, solver timestep.

            float3 Field(float2 uv)
            {
                return tex2D(_MainTex, uv).rgb;
            }

            float2 Flux(float3 current, float3 neighbor)
            {
                return (neighbor.rg - current.rg) * max(current.b, neighbor.b);
            }

            float4 Fragment(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                float2 metres = uv * _Surface.xy;
                float dt = _Ripple.w;
                float3 before = Field(uv);
                float2 away = metres - _Ripple.xy * _Surface.xy;
                float distanceFromImpact = length(away);
                float2 radial = away * rsqrt(max(dot(away, away), 0.01));
                float push = _Ripple.z * exp(-dot(away, away) * 1.42) * smoothstep(0.0, 0.22, distanceFromImpact);
                float upstreamWet = Field(uv - _Surface.zw * dt).b;
                float2 transport = (_Surface.zw + radial * push / _Surface.xy) * dt * max(before.b, upstreamWet);
                float2 sampleUV = uv - transport;
                float2 step = abs(_MainTex_TexelSize.xy);
                float3 centre = Field(sampleUV);
                float3 left = Field(sampleUV - float2(step.x, 0));
                float3 right = Field(sampleUV + float2(step.x, 0));
                float3 down = Field(sampleUV - float2(0, step.y));
                float3 up = Field(sampleUV + float2(0, step.y));

                float relaxation = min(0.20, dt * 6.4);
                float2 pigment = centre.rg + relaxation * (Flux(centre, left) + Flux(centre, right) +
                    Flux(centre, down) + Flux(centre, up));
                float neighborWet = (left.b + right.b + down.b + up.b) * 0.25;
                float wetness = saturate(lerp(centre.b, neighborWet, relaxation) - dt * 0.32);

                // Removing a fixed thickness preserves the local pigment ratio. Thin edges
                // vanish first while overlapping deposits share one continuous pooled field.
                pigment = max(pigment, 0.0);
                float thickness = pigment.x + pigment.y;
                pigment *= max(0.0, thickness - dt * 0.06) / max(thickness, 0.0001);

                if (_Deposit.y > 0.0)
                {
                    float2 start = _Segment.xy * _Surface.xy;
                    float2 end = _Segment.zw * _Surface.xy;
                    float2 axis = end - start;
                    float t = saturate(dot(metres - start, axis) / max(dot(axis, axis), 0.00001));
                    float2 offset = metres - (start + t * axis);
                    float radius = _Deposit.x + 0.025 * sin(dot(metres, float2(3.8, 2.3))) +
                        0.014 * sin(dot(metres, float2(-7.0, 5.1)));
                    float brush = 1.0 - smoothstep(radius - 0.16, radius + 0.055, length(offset));
                    pigment += float2(_Deposit.z, 1.0 - _Deposit.z) * (brush * _Deposit.y);
                    wetness = max(wetness, brush);
                }

                // Keep the mixing ratio when a pool reaches its maximum thickness.
                pigment *= min(1.0, 1.45 / max(pigment.x + pigment.y, 0.0001));
                return float4(pigment, wetness, 1.0);
            }
            ENDHLSL
        }
    }
}
