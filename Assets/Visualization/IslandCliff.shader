Shader "Little Witches/Island Cliff"
{
    Properties
    {
        _BaseColor("Tint", Color) = (0.8,0.85,0.7,1)
        _Sides("Stone And Dirt", 2D) = "white" {}
        _Top("Moss", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_Sides); SAMPLER(sampler_Sides);
            TEXTURE2D(_Top); SAMPLER(sampler_Top);
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float fog:TEXCOORD2; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS); o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half3 n=normalize(i.normalWS); float3 p=i.positionWS*0.25;
                half3 w=pow(abs(n),4); w/=max(w.x+w.y+w.z,0.001);
                half3 sideX=SAMPLE_TEXTURE2D(_Sides,sampler_Sides,p.zy).rgb;
                half3 sideZ=SAMPLE_TEXTURE2D(_Sides,sampler_Sides,p.xy).rgb;
                half3 top=SAMPLE_TEXTURE2D(_Top,sampler_Top,p.xz).rgb;
                half3 albedo=(sideX*w.x+sideZ*w.z+top*w.y)*_BaseColor.rgb;
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 light=max(SampleSH(n),half3(0.20,0.25,0.18))+sun.color*saturate(dot(n,sun.direction))*sun.shadowAttenuation;
                return half4(MixFog(albedo*light,i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
