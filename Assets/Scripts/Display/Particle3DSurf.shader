Shader"Instanced/Particle3DAir" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
	SubShader{
		Tags { "RenderType" = "Transparent" }
		LOD 200

		Blend
SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#pragma surface surf Standard addshadow fullforwardshadows alpha:fade vertex:vert
		#pragma multi_compile_instancing
		#pragma instancing_options procedural:setup

sampler2D _MainTex;

struct Input
{
    float2 uv_MainTex;
    float4 colour;
    float3 worldPos;
};

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			StructuredBuffer<float3> Positions;
			StructuredBuffer<float3> Velocities;
#endif

SamplerState linear_clamp_sampler;
float velocityMax;

float scale;
float3 colour;

sampler2D ColourMap;

void vert(inout appdata_full v, out Input o)
{
    UNITY_INITIALIZE_OUTPUT(Input, o);
    o.uv_MainTex = v.texcoord.xy;

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			float speed = length(Velocities[unity_InstanceID]);
			float speedT = saturate(speed / velocityMax);
			float colT = speedT;
			o.colour = tex2Dlod(ColourMap, float4(colT, 0.5, 0, 0));
#endif
}

void setup()
{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			float3 pos = Positions[unity_InstanceID];

			unity_ObjectToWorld._11_21_31_41 = float4(scale, 0, 0, 0);
			unity_ObjectToWorld._12_22_32_42 = float4(0, scale, 0, 0);
			unity_ObjectToWorld._13_23_33_43 = float4(0, 0, scale, 0);
			unity_ObjectToWorld._14_24_34_44 = float4(pos, 1);
			unity_WorldToObject = unity_ObjectToWorld;
			unity_WorldToObject._14_24_34 *= -1;
			unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;

#endif
}

half _Glossiness;
half _Metallic;

void surf(Input IN, inout SurfaceOutputStandard o)
{
    o.Albedo = IN.colour.rgb;
    o.Metallic = _Metallic;
    o.Smoothness = _Glossiness;
			// Adjust alpha based on color intensity to represent air particles
    o.Alpha = IN.colour.a * 0.1; // Make the particles more transparent to represent air
}
		ENDCG
	}
FallBack"Diffuse"
}
