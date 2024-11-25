Shader "Custom/ContainerShaderMask"
{
    Properties
    {
        [IntRange] _StencilID ("Stencil ID", Range(0,255)) = 0
    }

    SubShader {
        Tags { "RenderType"="Geometry-1" }
        LOD 200

        ColorMask 0
        ZWrite Off

        Stencil
        {
            Ref [_StencilID]
            Comp Always
            Pass Replace
        }  

        CGPROGRAM
        #pragma surface surf Standard
        #pragma target 3.0

        struct Input
        {
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Alpha = fixed4(1,1,1,1);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
