// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Fast Approximate Anti-aliasing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
        #pragma fragmentoption ARB_precision_hint_fastest

        #if defined(SHADER_API_PS3)
            #define FXAA_PS3 1

            // Shaves off 2 cycles from the shader
            #define FXAA_EARLY_EXIT 0
        #elif defined(SHADER_API_XBOX360)
            #define FXAA_360 1

            // Shaves off 10ms from the shader's execution time
            #define FXAA_EARLY_EXIT 1
        #else
            #define FXAA_PC 1
        #endif

        #define FXAA_HLSL_3 1
        #define FXAA_QUALITY__PRESET 39

        #define FXAA_GREEN_AS_LUMA 1

        #pragma target 3.0
		#include "UnityCG.cginc"
        #include "FXAA3.cginc"

        float4 _MainTex_TexelSize;
        float3 _QualitySettings;
        float4 _ConsoleSettings;
		half4 _MainTex_ST;

        UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);

        struct Input
        {
            float4 position : POSITION;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varying
        {
            float4 position : SV_POSITION;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
		    UNITY_VERTEX_OUTPUT_STEREO
        };

        Varying vertex(Input input)
        {
            Varying output;

            UNITY_SETUP_INSTANCE_ID(input);
		    UNITY_TRANSFER_INSTANCE_ID(input, output);
		    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            output.position = UnityObjectToClipPos(input.position);
			output.uv = UnityStereoScreenSpaceUVAdjust(input.uv, _MainTex_ST);

            return output;
        }

        float calculateLuma(float4 color)
        {
            return color.g * 1.963211 + color.r;
        }

        fixed4 fragment(Varying input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            const float4 consoleUV = input.uv.xyxy + .5 * float4(-_MainTex_TexelSize.xy, _MainTex_TexelSize.xy);
            const float4 consoleSubpixelFrame = _ConsoleSettings.x * float4(-1., -1., 1., 1.) *
                _MainTex_TexelSize.xyxy;

            const float4 consoleSubpixelFramePS3 = float4(-2., -2., 2., 2.) * _MainTex_TexelSize.xyxy;
            const float4 consoleSubpixelFrameXBOX = float4(8., 8., -4., -4.) * _MainTex_TexelSize.xyxy;

            #if defined(SHADER_API_XBOX360)
                const float4 consoleConstants = float4(1., -1., .25, -.25);
            #else
                const float4 consoleConstants = float4(0., 0., 0., 0.);
            #endif

            return FxaaPixelShader(input.uv, consoleUV, _MainTex, _MainTex, _MainTex, _MainTex_TexelSize.xy,
                consoleSubpixelFrame, consoleSubpixelFramePS3, consoleSubpixelFrameXBOX,
                _QualitySettings.x, _QualitySettings.y, _QualitySettings.z, _ConsoleSettings.y, _ConsoleSettings.z,
                _ConsoleSettings.w, consoleConstants);
        }
    ENDCG

    SubShader
    {
        ZTest Always Cull Off ZWrite Off
        Fog { Mode off }

        Pass
        {
            CGPROGRAM
                #pragma vertex vertex
                #pragma fragment fragment

                #include "UnityCG.cginc"
            ENDCG
        }
    }
}
