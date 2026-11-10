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
                float2 transport = (_Surface.zw + radial * push / _Surface.xy) * dt * before.b;
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
                float wetness = saturate(lerp(centre.b, neighborWet, relaxation) - dt * 0.60);

                if (_Deposit.y > 0.0)
                {
                    float2 start = _Segment.xy * _Surface.xy;
                    float2 end = _Segment.zw * _Surface.xy;
                    float2 axis = end - start;
                    float t = saturate(dot(metres - start, axis) / max(dot(axis, axis), 0.00001));
                    float2 offset = metres - (start + t * axis);
                    float radius = _Deposit.x + 0.047 * sin(dot(metres, float2(8.8, 5.3))) +
                        0.026 * sin(dot(metres, float2(-15.0, 11.1)));

                    float bead = 0.0;
                    if (_Deposit.w > 0.0)
                    {
                        float angle = atan2(offset.y, offset.x + 0.0001);
                        float phase = angle * 6.0 + dot(end, float2(2.3, 1.7));
                        float lobes = 0.5 + 0.5 * sin(phase);
                        float tongues = pow(lobes, 5.0);
                        radius += _Deposit.w * (tongues * 0.24 + 0.038 * cos(angle * 9.0));
                        float tip = radius + 0.14 + 0.075 * cos(angle * 3.0);
                        bead = smoothstep(0.94, 0.997, lobes) * (1.0 - smoothstep(0.015, 0.055, abs(length(offset) - tip)));
                        bead *= smoothstep(0.50, 0.85, _Deposit.w) * 0.82;
                    }

                    float brush = 1.0 - smoothstep(radius - 0.11, radius + 0.055, length(offset));
                    brush = max(brush, bead);
                    float fill = brush * _Deposit.y;
                    pigment += float2(_Deposit.z, 1.0 - _Deposit.z) * fill;
                    wetness = max(wetness, brush);
                }

                // Preserve the pigment ratio when the pool reaches its maximum thickness.
                pigment = max(pigment, 0.0);
                pigment *= min(1.0, 1.45 / max(pigment.x + pigment.y, 0.0001));
                return float4(pigment, wetness, 1.0);
            }
            ENDHLSL
        }
    }
}
