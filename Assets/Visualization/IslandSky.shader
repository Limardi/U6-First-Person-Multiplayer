Shader "Little Witches/Island Sky"
{
    Properties
    {
        _Top("Upper Sky", Color) = (0.28,0.55,0.69,1)
        _Horizon("Horizon", Color) = (0.78,0.87,0.82,1)
        _Bottom("Below Clouds", Color) = (0.50,0.68,0.72,1)
        _CloudColor("Cloud Highlights", Color) = (1,0.95,0.84,1)
        _CloudShadow("Cloud Shadows", Color) = (0.57,0.72,0.83,1)
        _CloudCoverage("Cloud Coverage", Range(0,1)) = 0.55
        _SunDirection("Sun Direction", Vector) = (0.48,0.62,-0.63,0)
        [HDR] _SunColor("Sun Glow", Color) = (2.5,1.9,1.1,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            half4 _Top, _Horizon, _Bottom, _CloudColor, _CloudShadow, _SunColor;
            float4 _SunDirection;
            float _CloudCoverage;
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.positionCS.z=o.positionCS.w*UNITY_RAW_FAR_CLIP_VALUE;
                o.direction=i.positionOS.xyz; return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 cell=floor(p), u=frac(p); u=u*u*(3-2*u);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),u.x),
                    lerp(Hash(cell+float2(0,1)),Hash(cell+1),u.x),u.y);
            }
            float Billows(float2 p)
            {
                return Noise(p)*0.55+Noise(p*2.1+7.3)*0.28+Noise(p*4.3+19.7)*0.12+Noise(p*8.1)*0.05;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float3 direction=normalize(i.direction);
                float y=direction.y;
                float sunFacing=saturate(dot(direction,normalize(_SunDirection.xyz)));
                half3 color=y>0 ? lerp(_Horizon.rgb,_Top.rgb,pow(saturate(y),0.42)) : lerp(_Horizon.rgb,_Bottom.rgb,saturate(-y*1.3));
                color+=_SunColor.rgb*pow(sunFacing,48)*0.12;
                float2 cloudUV=direction.xz/(abs(y)+0.19)*2.2+float2(3.7,9.1);
                float density=Billows(cloudUV);
                float threshold=lerp(0.76,0.32,_CloudCoverage);
                float cloud=smoothstep(threshold,threshold+0.16,density);
                float upperMask=smoothstep(0.025,0.15,y)*(1-smoothstep(0.84,1,y));
                float belowMask=smoothstep(0.01,0.20,-y);
                float shade=saturate((density-threshold)*3+Noise(cloudUV+float2(-0.2,0.3))*0.45);
                half3 cloudColor=lerp(_CloudShadow.rgb,_CloudColor.rgb,shade);
                color=lerp(color,cloudColor,cloud*max(upperMask,belowMask)*0.92);
                float sunDisc=smoothstep(0.9992,0.9996,sunFacing);
                color+=_SunColor.rgb*sunDisc*(1-cloud*upperMask*0.75);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
