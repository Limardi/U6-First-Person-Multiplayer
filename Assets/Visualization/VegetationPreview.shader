Shader "Little Witches/Vegetation Preview"
{
    Properties
    {
        _LeafTex("Leaf Texture", 2D) = "white" {}
        _TunkTex("Trunk Texture", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.45
        _AmbientFloor("Minimum Ambient Fill", Color) = (0,0,0,1)
        _Translucency("Leaf Backlight", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_LeafTex); SAMPLER(sampler_LeafTex);
        TEXTURE2D(_TunkTex); SAMPLER(sampler_TunkTex);
        CBUFFER_START(UnityPerMaterial)
        float4 _LeafTex_ST;
        float4 _TunkTex_ST;
        float _Cutoff;
        half4 _AmbientFloor;
        float _Translucency;
        CBUFFER_END
        struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float4 color:COLOR; };
        struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 positionWS:TEXCOORD2; float leaf:TEXCOORD3; float fogFactor:TEXCOORD4; };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = input.uv;
            // The supplied biome meshes encode leaf-versus-trunk identity in blue.
            output.leaf = step(0.5, input.color.b);
            output.fogFactor = ComputeFogFactor(output.positionCS.z);
            return output;
        }
        half4 Albedo(Varyings input)
        {
            half4 leaf = SAMPLE_TEXTURE2D(_LeafTex, sampler_LeafTex, input.uv * _LeafTex_ST.xy + _LeafTex_ST.zw);
            half4 trunk = SAMPLE_TEXTURE2D(_TunkTex, sampler_TunkTex, input.uv * _TunkTex_ST.xy + _TunkTex_ST.zw);
            half4 color = lerp(trunk, leaf, input.leaf);
            clip(color.a - _Cutoff);
            return color;
        }
        ENDHLSL
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            half4 Frag(Varyings input, bool frontFace:SV_IsFrontFace):SV_Target
            {
                half4 color = Albedo(input);
                float3 normal = normalize(input.normalWS) * (frontFace ? 1 : -1);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half backlight = pow(saturate(dot(-normal, light.direction)), 2) * input.leaf * _Translucency;
                half3 illumination = max(SampleSH(normal), _AmbientFloor.rgb) + light.color *
                    ((0.18 + saturate(dot(normal, light.direction)) * 0.82) * light.shadowAttenuation + backlight);
                color.rgb *= illumination;
                return half4(MixFog(color.rgb, input.fogFactor), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDepth
            half4 FragDepth(Varyings input):SV_Target { Albedo(input); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDepth
            half4 FragDepth(Varyings input):SV_Target { Albedo(input); return 0; }
            ENDHLSL
        }
    }
}
