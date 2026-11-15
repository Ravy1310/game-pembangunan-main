// Cozy Glade - Ghost / placement preview & hover highlight (URP). Fresnel glow, soft pulse, transparent.
Shader "Cozy/Ghost"
{
    Properties
    {
        _Color("Color", Color) = (0.45,0.85,0.5,0.45)
        _FresnelPower("Fresnel Power", Range(0.5,6)) = 2.2
        _FresnelBoost("Fresnel Boost", Range(0,1)) = 0.35
        _PulseAmount("Pulse Amount", Range(0,1)) = 0.25
        _PulseSpeed("Pulse Speed", Float) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Name "Ghost"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _FresnelPower;
                half _FresnelBoost;
                half _PulseAmount;
                half _PulseSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float fres = pow(1.0 - saturate(dot(N, V)), _FresnelPower);
                float pulse = 1.0 + _PulseAmount * sin(_Time.y * _PulseSpeed) * 0.5;
                half a = saturate(_Color.a * (0.6 + 0.4 * fres) * pulse);
                half3 c = _Color.rgb + fres * _FresnelBoost;
                return half4(c, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
