Shader "Custom/PSX/PSX_Global_PostProcess"
{
    Properties
    {
        [Header(Pixelation)]
        [Toggle] _EnablePixelation ("Enable Pixelation", Float) = 1
        _PixelResolution ("Pixel Resolution", Vector) = (320, 240, 0, 0)

        [Header(Color Quantization)]
        [Toggle] _EnableColorQuantization ("Enable Color Quantization", Float) = 1
        _ColorBits ("Color Bits Per Channel", Range(1, 8)) = 5

        [Header(Dithering)]
        [Toggle] _EnableDither ("Enable Dither", Float) = 1
        _DitherSpread ("Dither Spread", Range(0, 4)) = 1
        _DitherGamma ("Dither Gamma", Range(0.1, 4)) = 1

        [Header(Final Mix)]
        _EffectStrength ("Effect Strength", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "PSXGlobalPostProcess"

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _EnablePixelation;
                float4 _PixelResolution;

                float _EnableColorQuantization;
                float _ColorBits;

                float _EnableDither;
                float _DitherSpread;
                float _DitherGamma;

                float _EffectStrength;
            CBUFFER_END

            float GetDitherValue(float2 screenPosition)
            {
                int x = ((int)screenPosition.x) & 3;
                int y = ((int)screenPosition.y) & 3;

                float value = 0.0;

                if (y == 0)
                {
                    if (x == 0) value = -4.0;
                    else if (x == 1) value = 0.0;
                    else if (x == 2) value = -3.0;
                    else value = 1.0;
                }
                else if (y == 1)
                {
                    if (x == 0) value = 2.0;
                    else if (x == 1) value = -2.0;
                    else if (x == 2) value = 3.0;
                    else value = -1.0;
                }
                else if (y == 2)
                {
                    if (x == 0) value = -3.0;
                    else if (x == 1) value = 1.0;
                    else if (x == 2) value = -4.0;
                    else value = 0.0;
                }
                else
                {
                    if (x == 0) value = 3.0;
                    else if (x == 1) value = -1.0;
                    else if (x == 2) value = 2.0;
                    else value = -2.0;
                }

                return value;
            }

            float3 ApplyColorQuantizationAndDither(float3 color, float2 screenPosition)
            {
                float gammaValue = max(_DitherGamma, 0.0001);

                float3 workingColor = pow(saturate(color), 1.0 / gammaValue);

                float noise = 0.0;

                if (_EnableDither > 0.5)
                {
                    noise = GetDitherValue(screenPosition) * _DitherSpread;
                }

                float bits = clamp(round(_ColorBits), 1.0, 8.0);
                float maxValue = exp2(bits) - 1.0;

                float3 color255 = workingColor * 255.0;

                color255 = round(color255 + noise);
                color255 = clamp(color255, 0.0, 255.0);

                float3 quantized = round((color255 / 255.0) * maxValue) / maxValue;

                quantized = pow(saturate(quantized), gammaValue);

                return saturate(quantized);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                if (_EnablePixelation > 0.5)
                {
                    float2 targetResolution = max(_PixelResolution.xy, float2(1.0, 1.0));

                    uv = floor(uv * targetResolution) / targetResolution;
                    uv += 0.5 / targetResolution;
                }

                float4 originalColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_PointClamp,
                    uv
                );

                float3 finalColor = originalColor.rgb;

                if (_EnableColorQuantization > 0.5)
                {
                    finalColor = ApplyColorQuantizationAndDither(
                        finalColor,
                        input.positionCS.xy
                    );
                }

                finalColor = lerp(
                    originalColor.rgb,
                    finalColor,
                    saturate(_EffectStrength)
                );

                return half4(finalColor, originalColor.a);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
