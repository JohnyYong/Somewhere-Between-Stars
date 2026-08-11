Shader "Custom/SlimeSphereURP"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.35, 0.85, 0.35, 1)
        _BaseMap ("Albedo (RGB)", 2D) = "white" {}

        [Header(Wobble)]
        _WobbleAmount ("Wobble Amount (0 = smooth, 1 = max jiggle)", Range(0, 1)) = 0.3
        _WobbleSpeed ("Wobble Speed", Range(0, 10)) = 2.0
        _WobbleFrequency ("Wobble Frequency", Range(0.1, 10)) = 2.5
        _WobbleScale ("Wobble Displacement Scale", Range(0, 1)) = 0.25

        [Header(Surface)]
        _Smoothness ("Smoothness", Range(0,1)) = 0.85
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _RimColor ("Rim / Fresnel Color", Color) = (0.6, 1.0, 0.6, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Basic URP keywords so it responds to main light shadows / additional lights
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float3 positionWS  : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _RimColor;
                half _Smoothness;
                half _Metallic;
                half _RimPower;
                float _WobbleAmount;
                float _WobbleSpeed;
                float _WobbleFrequency;
                float _WobbleScale;
            CBUFFER_END

            // Same object-space triple-sine wobble as the Built-in version,
            // so behavior matches between pipelines.
            float3 ApplyWobble(float3 positionOS, float3 normalOS)
            {
                float t = _Time.y * _WobbleSpeed;

                float wave1 = sin(positionOS.x * _WobbleFrequency + t);
                float wave2 = cos(positionOS.y * _WobbleFrequency * 1.37 + t * 0.8);
                float wave3 = sin(positionOS.z * _WobbleFrequency * 0.91 + t * 1.23);

                float wobble = wave1 * wave2 * wave3;
                float displacement = wobble * _WobbleAmount * _WobbleScale;

                return positionOS + normalOS * displacement;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 displacedPosOS = ApplyWobble(IN.positionOS.xyz, IN.normalOS);

                VertexPositionInputs posInputs = GetVertexPositionInputs(displacedPosOS);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 albedo = texColor.rgb * _BaseColor.rgb;

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // Main directional light (URP)
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3 lightColor = mainLight.color * mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = albedo * lightColor * NdotL;

                // Simple specular (Blinn-Phong) driven by smoothness
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                half spec = pow(saturate(dot(normalWS, halfDir)), lerp(8, 128, _Smoothness));
                half3 specular = lightColor * spec * lerp(0.04, 1.0, _Metallic);

                // Ambient / indirect light so it's not pitch black in shadow
                half3 ambient = SampleSH(normalWS) * albedo;

                // Fresnel rim highlight
                half rim = 1.0 - saturate(dot(viewDirWS, normalWS));
                half3 rimEmission = _RimColor.rgb * pow(rim, _RimPower) * 0.5;

                half3 color = diffuse + specular + ambient + rimEmission;

                return half4(color, texColor.a * _BaseColor.a);
            }
            ENDHLSL
        }

        // Lets the sphere both cast and receive shadows correctly with the wobble applied
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _WobbleAmount;
            float _WobbleSpeed;
            float _WobbleFrequency;
            float _WobbleScale;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float3 ApplyWobbleShadow(float3 positionOS, float3 normalOS)
            {
                float t = _Time.y * _WobbleSpeed;
                float wave1 = sin(positionOS.x * _WobbleFrequency + t);
                float wave2 = cos(positionOS.y * _WobbleFrequency * 1.37 + t * 0.8);
                float wave3 = sin(positionOS.z * _WobbleFrequency * 0.91 + t * 1.23);
                float wobble = wave1 * wave2 * wave3;
                float displacement = wobble * _WobbleAmount * _WobbleScale;
                return positionOS + normalOS * displacement;
            }

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                float3 displaced = ApplyWobbleShadow(IN.positionOS.xyz, IN.normalOS);
                VertexPositionInputs posInputs = GetVertexPositionInputs(displaced);
                OUT.positionHCS = posInputs.positionCS;
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
