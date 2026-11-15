// Cozy Glade - Toon Lit (URP). 3-step soft ramp, tinted shadows, rim light, emission, optional world-space UV and wind sway.
Shader "Cozy/ToonLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [Toggle(_WORLDUV_ON)] _WorldUV("World-Space UV (walls / roofs)", Float) = 0
        _WorldUVScale("World UV Scale", Float) = 0.5
        _ShadowColor("Shadow Tint", Color) = (0.42,0.40,0.62,1)
        _Threshold1("Ramp Threshold 1", Range(0,1)) = 0.38
        _Threshold2("Ramp Threshold 2", Range(0,1)) = 0.66
        _RampSmooth("Ramp Softness", Range(0.001,0.3)) = 0.07
        _RimColor("Rim Color", Color) = (1,0.9,0.72,1)
        _RimPower("Rim Power", Range(0.5,8)) = 3
        _RimStrength("Rim Strength", Range(0,1)) = 0.22
        [Toggle(_EMISSION)] _UseEmission("Use Emission Map", Float) = 0
        _EmissionMap("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color (0 = off)", Color) = (0,0,0,1)
        _WindStrength("Wind Strength", Range(0,0.5)) = 0
        _WindSpeed("Wind Speed", Float) = 1.6
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half   _WorldUV;
            float  _WorldUVScale;
            half4  _ShadowColor;
            half   _Threshold1;
            half   _Threshold2;
            half   _RampSmooth;
            half4  _RimColor;
            half   _RimPower;
            half   _RimStrength;
            half   _UseEmission;
            half4  _EmissionColor;
            half   _WindStrength;
            half   _WindSpeed;
        CBUFFER_END

        // gentle sway; bottom of the object (y = 0) stays fixed
        float3 CozyWind(float3 positionOS)
        {
            if (_WindStrength <= 0.0001) return positionOS;
            float3 wp = TransformObjectToWorld(positionOS);
            float h = max(positionOS.y, 0.0);
            float t = _Time.y * _WindSpeed + wp.x * 0.7 + wp.z * 0.5;
            float sway = sin(t) + 0.5 * sin(t * 2.3 + 1.7);
            positionOS.x += sway * _WindStrength * h * h * 0.08;
            positionOS.z += cos(t * 0.9) * _WindStrength * h * h * 0.05;
            return positionOS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _WORLDUV_ON
            #pragma shader_feature_local _EMISSION
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                float3 posOS = CozyWind(i.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(posOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                #ifdef _WORLDUV_ON
                    float3 an = abs(o.normalWS);
                    float2 wuv = an.y > 0.5 ? o.positionWS.xz : (an.x > an.z ? o.positionWS.zy : o.positionWS.xy);
                    o.uv = wuv * _WorldUVScale;
                #else
                    o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                #endif
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light L = GetMainLight(shadowCoord);
                float x = (dot(N, L.direction) * 0.5 + 0.5) * lerp(0.35, 1.0, L.shadowAttenuation);
                float s = _RampSmooth;
                float ramp = (smoothstep(_Threshold1 - s, _Threshold1 + s, x) + smoothstep(_Threshold2 - s, _Threshold2 + s, x)) * 0.5;
                half3 tint = lerp(_ShadowColor.rgb, half3(1, 1, 1), ramp);

                half3 col = tex.rgb * tint * L.color;
                col += tex.rgb * SampleSH(N) * 0.55;

                // extra lights (lanterns, campfire): soft, no hard banding
                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = i.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light al = GetAdditionalLight(lightIndex, i.positionWS, half4(1, 1, 1, 1));
                        float nl = saturate(dot(N, al.direction) * 0.5 + 0.5);
                        col += tex.rgb * al.color * (al.distanceAttenuation * al.shadowAttenuation * nl * 0.9);
                    LIGHT_LOOP_END
                #endif

                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower) * _RimStrength;
                col += _RimColor.rgb * rim * saturate(ramp + 0.3);

                #ifdef _EMISSION
                    col += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                #endif

                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;

            struct SAttr { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct SVary { float4 positionCS : SV_POSITION; };

            SVary ShadowVert(SAttr i)
            {
                SVary o;
                UNITY_SETUP_INSTANCE_ID(i);
                float3 posWS = TransformObjectToWorld(CozyWind(i.positionOS.xyz));
                float3 nWS = TransformObjectToWorldNormal(i.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, lightDir));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs;
                return o;
            }
            half4 ShadowFrag(SVary i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DAttr { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct DVary { float4 positionCS : SV_POSITION; };
            DVary DepthVert(DAttr i)
            {
                DVary o; UNITY_SETUP_INSTANCE_ID(i);
                o.positionCS = TransformObjectToHClip(CozyWind(i.positionOS.xyz));
                return o;
            }
            half DepthFrag(DVary i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex NVert
            #pragma fragment NFrag
            #pragma multi_compile_instancing

            struct NAttr { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct NVary { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            NVary NVert(NAttr i)
            {
                NVary o; UNITY_SETUP_INSTANCE_ID(i);
                o.positionCS = TransformObjectToHClip(CozyWind(i.positionOS.xyz));
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }
            half4 NFrag(NVary i) : SV_Target { return half4(normalize(i.normalWS), 0.0); }
            ENDHLSL
        }
    }
    Fallback Off
}
