Shader "Custom/VertexColorShader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        // Add other properties as needed
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert

        struct Input
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD0;
            fixed4 color : COLOR; // Vertex color attribute
        };

        void vert(inout appdata_full v)
        {
            // Pass vertex color to the fragment shader
            v.color = v.color;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.color.rgb * _Color.rgb; // Use vertex color for the mesh color
            o.Alpha = 1; // Optionally, you can set the alpha value
        }
        ENDCG
    }
    FallBack "Diffuse"
}
