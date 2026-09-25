Shader "PiratePrototype/WaterFoamStamp"
{
    Properties
    {
        _Foam_Texture("Foam footprint", 2D) = "white" {}
        [HideInInspector] _AffectFoam("Affect foam", Float) = 1
        [HideInInspector] _AffectDeformation("Affect deformation", Float) = 0
    }
    SubShader
    {
        // HDRP 17.3 WaterDecal's atlas contract: named Foam pass, G/B foam channels.
        Tags { "RenderPipeline"="HDRenderPipeline" "ShaderGraphTargetId"="WaterDecalSubTarget" }
        Pass
        {
            Name "Foam"
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            TEXTURE2D(_Foam_Texture);
            SAMPLER(sampler_Foam_Texture);
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(uint id : SV_VertexID)
            {
                Varyings output;
                output.uv = float2((id << 1) & 2, id & 2);
                output.positionCS = float4(output.uv * 2 - 1, 0, 1);
                return output;
            }
            float4 Frag(Varyings input) : SV_Target
            {
                return float4(0, SAMPLE_TEXTURE2D(_Foam_Texture, sampler_Foam_Texture, input.uv).r, 0, 0);
            }
            ENDHLSL
        }
    }
}
