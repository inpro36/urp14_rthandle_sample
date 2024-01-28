Shader "InPro/UberPost"
{
    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "UberPost"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            #pragma vertex Vert
            #pragma fragment frag

            half _AberrationIntensity;
            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                /// 色収差. ///
                half2 uvBase = i.texcoord - 0.5h;
                // R値を拡大したものに置き換える
                half2 uvR = uvBase * (1.0h - _AberrationIntensity * 2.0h) + 0.5h;
                color.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvR);
                // G値を拡大したものに置き換える
                half2 uvG = uvBase * (1.0h - _AberrationIntensity) + 0.5h;
                color.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvG);

                return color;
            }
            ENDHLSL
        }
    }
}