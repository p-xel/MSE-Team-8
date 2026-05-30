Shader "Custom/PSX/PSX_Lit_Material"
{
    Properties
    {
        [MainTexture] _BaseMap ("Color Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)

        [Header(PSX Geometry)]
        [Toggle] _Billboard ("Billboard", Float) = 0
        [Toggle] _VertexSnapping ("Vertex Snapping", Float) = 1
        [Toggle] _AffineTextureMapping ("Affine Texture Mapping", Float) = 1

        [Header(Scene Lighting)]
        [Toggle] _UseSceneLighting ("Use Scene Lighting", Float) = 1
        _LightIntensity ("Light Intensity", Float) = 1
        _AmbientLight ("Ambient Light", Color) = (0.2, 0.2, 0.2, 1)

        [Header(Optional Object Fog)]
        [Toggle] _AddFog ("Add Object Fog", Float) = 0
        _FogColor ("Fog Color", Color) = (0.42, 0.42, 0.45, 1)
        _FogStartEnd ("Fog Start / End", Vector) = (10, 100, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Tint;

                float _Billboard;
                float _VertexSnapping;
                float _AffineTextureMapping;

                float _UseSceneLighting;
                float _LightIntensity;
                float4 _AmbientLight;

                float _AddFog;
                float4 _FogColor;
                float4 _FogStartEnd;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float2 uvPayload     : TEXCOORD0;
                float4 colorPayload  : TEXCOORD1;
                float fogPayload     : TEXCOORD2;
                float clipW          : TEXCOORD3;
                float3 lighting      : TEXCOORD4;
            };

            void GetBillboardBasis(
                out float3 rightWS,
                out float3 upWS,
                out float3 forwardWS
            )
            {
                rightWS = normalize(float3(
                    UNITY_MATRIX_I_V[0].x,
                    UNITY_MATRIX_I_V[1].x,
                    UNITY_MATRIX_I_V[2].x
                ));

                upWS = float3(0.0, 1.0, 0.0);

                forwardWS = normalize(float3(
                    UNITY_MATRIX_I_V[0].z,
                    UNITY_MATRIX_I_V[1].z,
                    UNITY_MATRIX_I_V[2].z
                ));
            }

            float4 SnapClipPositionToPixelGrid(float4 clipPos)
            {
                float2 ndc = clipPos.xy / clipPos.w;

                float2 pixelPos = (ndc * 0.5 + 0.5) * _ScreenParams.xy;
                pixelPos = round(pixelPos);

                ndc = (pixelPos / _ScreenParams.xy) * 2.0 - 1.0;

                clipPos.xy = ndc * clipPos.w;

                return clipPos;
            }

            float3 CalculatePSXVertexLighting(float3 worldPos, float3 normalWS)
            {
                float3 n = normalize(normalWS);
                float3 lighting = _AmbientLight.rgb;

                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
                Light mainLight = GetMainLight(shadowCoord);

                float mainNdotL = saturate(dot(n, mainLight.direction));

                lighting +=
                    mainLight.color *
                    mainNdotL *
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation;

                #if defined(_ADDITIONAL_LIGHTS)
                    uint additionalLightCount = GetAdditionalLightsCount();

                    for (uint lightIndex = 0u; lightIndex < additionalLightCount; lightIndex++)
                    {
                        Light light = GetAdditionalLight(lightIndex, worldPos);

                        float ndotl = saturate(dot(n, light.direction));

                        lighting +=
                            light.color *
                            ndotl *
                            light.distanceAttenuation *
                            light.shadowAttenuation;
                    }
                #endif

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    lighting += VertexLighting(worldPos, n);
                #endif

                lighting *= _LightIntensity;

                return max(lighting, 0.0);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 worldPos;
                float3 normalWS;

                if (_Billboard > 0.5)
                {
                    float3 rightWS;
                    float3 upWS;
                    float3 forwardWS;

                    GetBillboardBasis(rightWS, upWS, forwardWS);

                    float3 pivotWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));

                    worldPos =
                        pivotWS +
                        rightWS   * input.positionOS.x +
                        upWS      * input.positionOS.y +
                        forwardWS * input.positionOS.z;

                    normalWS = normalize(
                        rightWS   * input.normalOS.x +
                        upWS      * input.normalOS.y +
                        forwardWS * input.normalOS.z
                    );
                }
                else
                {
                    worldPos = TransformObjectToWorld(input.positionOS.xyz);
                    normalWS = TransformObjectToWorldNormal(input.normalOS);
                }

                float4 clipPos = TransformWorldToHClip(worldPos);

                if (_VertexSnapping > 0.5)
                {
                    clipPos = SnapClipPositionToPixelGrid(clipPos);
                }

                output.positionCS = clipPos;
                output.clipW = clipPos.w;

                float2 uv = TRANSFORM_TEX(input.uv, _BaseMap);
                float4 vertexColor = input.color;

                float3 lighting = float3(1.0, 1.0, 1.0);

                if (_UseSceneLighting > 0.5)
                {
                    lighting = CalculatePSXVertexLighting(worldPos, normalWS);
                }

                float viewDistance = distance(GetCameraPositionWS(), worldPos);

                float fogStart = _FogStartEnd.x;
                float fogEnd = _FogStartEnd.y;

                float fogFactor = saturate(
                    (viewDistance - fogStart) /
                    max(fogEnd - fogStart, 0.0001)
                );

                if (_AffineTextureMapping > 0.5)
                {
                    uv *= output.clipW;
                    vertexColor *= output.clipW;
                    fogFactor *= output.clipW;
                    lighting *= output.clipW;
                }

                output.uvPayload = uv;
                output.colorPayload = vertexColor;
                output.fogPayload = fogFactor;
                output.lighting = lighting;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uvPayload;
                float4 vertexColor = input.colorPayload;
                float fogFactor = input.fogPayload;
                float3 lighting = input.lighting;

                if (_AffineTextureMapping > 0.5)
                {
                    float w = max(input.clipW, 0.0001);

                    uv /= w;
                    vertexColor /= w;
                    fogFactor /= w;
                    lighting /= w;
                }

                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);

                float3 albedo = tex.rgb * vertexColor.rgb * _Tint.rgb;
                float3 finalColor = albedo * lighting;

                if (_AddFog > 0.5)
                {
                    finalColor = lerp(finalColor, _FogColor.rgb, saturate(fogFactor));
                }

                return half4(finalColor, tex.a * vertexColor.a * _Tint.a);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Tint;

                float _Billboard;
                float _VertexSnapping;
                float _AffineTextureMapping;

                float _UseSceneLighting;
                float _LightIntensity;
                float4 _AmbientLight;

                float _AddFog;
                float4 _FogColor;
                float4 _FogStartEnd;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            void GetShadowBillboardBasis(
                out float3 rightWS,
                out float3 upWS,
                out float3 forwardWS
            )
            {
                rightWS = normalize(float3(
                    UNITY_MATRIX_I_V[0].x,
                    UNITY_MATRIX_I_V[1].x,
                    UNITY_MATRIX_I_V[2].x
                ));

                upWS = float3(0.0, 1.0, 0.0);

                forwardWS = normalize(float3(
                    UNITY_MATRIX_I_V[0].z,
                    UNITY_MATRIX_I_V[1].z,
                    UNITY_MATRIX_I_V[2].z
                ));
            }

            float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS)
            {
                float3 lightDirectionWS = _LightDirection;

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    lightDirectionWS = normalize(_LightPosition - positionWS);
                #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDirectionWS)
                );

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;

                float3 worldPos;
                float3 normalWS;

                if (_Billboard > 0.5)
                {
                    float3 rightWS;
                    float3 upWS;
                    float3 forwardWS;

                    GetShadowBillboardBasis(rightWS, upWS, forwardWS);

                    float3 pivotWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));

                    worldPos =
                        pivotWS +
                        rightWS   * input.positionOS.x +
                        upWS      * input.positionOS.y +
                        forwardWS * input.positionOS.z;

                    normalWS = normalize(
                        rightWS   * input.normalOS.x +
                        upWS      * input.normalOS.y +
                        forwardWS * input.normalOS.z
                    );
                }
                else
                {
                    worldPos = TransformObjectToWorld(input.positionOS.xyz);
                    normalWS = TransformObjectToWorldNormal(input.normalOS);
                }

                output.positionCS = GetShadowPositionHClip(worldPos, normalWS);

                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
