// Cozy Glade - Grass (URP). Vertical gradient root->tip, wind sway, receives main-light shadows. GPU-instancing friendly.
Shader "Cozy/Grass"
{
    Properties
    {
        _RootColor("Root Color", Color) = (0.31,0.54,0.29,1)
        _TipColor("Tip Color", Color) = (0.77,0.90,0.55,1)
        _BladeHeight("Blade Height (object space)", Float) = 0.36
        _ShadowColor("Shadow Tint", Color) = (0.50,0.52,0.72,1)
        _WindStrength("Wind Strength", Range(0,1)) = 0.35
        _WindSpeed("Wind Speed", Float) = 1.8
        _WindScale("Wind Wave Scale", Float) = 0.45
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RootColor;
                half4 _TipColor;
                float _BladeHeight;
                half4 _ShadowColor;
                half  _WindStrength;
                half  _WindSpeed;
                half  _WindScale;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half   height01   : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                float3 wp = TransformObjectToWorld(i.positionOS.xyz);
                float h = saturate(i.positionOS.y / max(_BladeHeight, 0.001));
                float t = _Time.y * _WindSpeed + (wp.x + wp.z * 0.7) * _WindScale;
                float wave = sin(t) + 0.45 * sin(t * 2.1 + wp.z * 0.9);
                float gust = 0.5 + 0.5 * sin(_Time.y * 0.45 + wp.x * 0.12);
                float2 dir = float2(1.0, 0.4);
                wp.xz += dir * wave * (0.04 + 0.08 * gust) * _WindStrength * h * h;
                o.positionWS = wp;
                o.positionCS = TransformWorldToHClip(wp);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.height01 = h;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 N = normalize(lerp(normalize(i.normalWS), float3(0, 1, 0), 0.75));
                Light L = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float lit = (dot(N, L.direction) * 0.5 + 0.5) * lerp(0.4, 1.0, L.shadowAttenuation);
                float ramp = smoothstep(0.38, 0.62, lit);
                half3 albedo = lerp(_RootColor.rgb, _TipColor.rgb, i.height01);
                half3 col = albedo * lerp(_ShadowColor.rgb, half3(1, 1, 1), ramp) * L.color;
                col += albedo * SampleSH(N) * 0.5;
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            Cull Off
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct DAttr { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct DVary { float4 positionCS : SV_POSITION; };
            DVary DepthVert(DAttr i) { DVary o; UNITY_SETUP_INSTANCE_ID(i); o.positionCS = TransformObjectToHClip(i.positionOS.xyz); return o; }
            half DepthFrag(DVary i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback Off
}
