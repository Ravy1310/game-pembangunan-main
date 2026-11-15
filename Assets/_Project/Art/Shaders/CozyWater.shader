// Cozy Glade - Pond water (URP). Radial depth tint, drifting sparkles, wobbling edge foam. Made for the round pond mesh (radius property).
Shader "Cozy/Water"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.50,0.77,0.84,1)
        _DeepColor("Deep Color", Color) = (0.35,0.64,0.75,1)
        _FoamColor("Foam Color", Color) = (0.92,0.97,0.97,1)
        _Alpha("Alpha", Range(0,1)) = 0.88
        _Radius("Pond Radius (object space)", Float) = 1.55
        _FoamWidth("Foam Width", Range(0.01,0.5)) = 0.14
        _WaveSpeed("Wave Speed", Float) = 0.6
        _SparkleScale("Sparkle Scale", Float) = 2.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                half _Alpha;
                float _Radius;
                half _FoamWidth;
                half _WaveSpeed;
                half _SparkleScale;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float2 posXZ : TEXCOORD1; half fogFactor : TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.posXZ = i.positionOS.xz;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float r = length(i.posXZ);
                float t01 = saturate(r / max(_Radius, 0.001));
                half3 col = lerp(_DeepColor.rgb, _ShallowColor.rgb, t01 * t01);
                float2 p = i.positionWS.xz * _SparkleScale + _Time.y * _WaveSpeed * float2(0.35, 0.22);
                float s = sin(p.x * 3.0 + sin(p.y * 2.3) * 1.5) * sin(p.y * 3.4 + sin(p.x * 1.7));
                col += smoothstep(0.72, 0.95, s) * 0.35;
                float ang = atan2(i.posXZ.y, i.posXZ.x);
                float wob = 0.5 + 0.5 * sin(ang * 7.0 + _Time.y * 1.2);
                float foamEdge = _Radius - _FoamWidth * (0.6 + 0.6 * wob);
                float foam = smoothstep(foamEdge, _Radius, r);
                col = lerp(col, _FoamColor.rgb, foam * 0.85);
                col = MixFog(col, i.fogFactor);
                return half4(col, _Alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
