Shader "LightweightPipeline/Terrain/Standard Terrain"
{
    Properties
    {
        // set by terrain engine
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3("Layer 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Splat2("Layer 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Splat1("Layer 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Splat0("Layer 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
        [HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
        [HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
        [HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}
        [HideInInspector][Gamma] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 0.5

        // used in fallback on old cards & base map
        [HideInInspector] _MainTex("BaseMap (RGB)", 2D) = "grey" {}
        [HideInInspector] _Color("Main Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue" = "Geometry-100" "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "False"}

        Pass
        {
            Name "TerrainLit"
            Tags { "LightMode" = "LightweightForward" }
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #pragma vertex SplatmapVert
            #pragma fragment SpatmapFragment

            #define _METALLICSPECGLOSSMAP 1
            #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1

            // -------------------------------------
            // Lightweight Pipeline keywords
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _VERTEX_LIGHTS
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile _ _SHADOWS_ENABLED
            #pragma multi_compile _ _LOCAL_SHADOWS_ENABLED
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _SHADOWS_CASCADE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #pragma multi_compile __ _TERRAIN_NORMAL_MAP

            #include "LWRP/ShaderLibrary/Terrain/InputSurfaceTerrain.hlsl"
            #include "LWRP/ShaderLibrary/Terrain/LightweightPassLitTerrain.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "LWRP/ShaderLibrary/InputSurfacePBR.hlsl"
            #include "LWRP/ShaderLibrary/LightweightPassShadow.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "LWRP/ShaderLibrary/InputSurfacePBR.hlsl"
            #include "LWRP/ShaderLibrary/LightweightPassDepthOnly.hlsl"
            ENDHLSL
        }
        //seongdae
        /*Pass
        {
            Name "LinearizedDepth"
            Tags { "LightMode" = "LinearizedDepth" }

            ZWrite On
            ZTest LEqual
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex LinearizedVertex
            #pragma fragment LinearizedFragment

            #include "LWRP/ShaderLibrary/InputSurfacePBR.hlsl"

            float4 _clipPlane;

            struct VertexInput
            {
                float4 vertex : POSITION;
            };

            struct VertexToFragment
            {
                float4 pos : SV_POSITION;
                float4 posVS : COLOR0;
            };

            VertexToFragment LinearizedVertex(VertexInput input)
            {
                VertexToFragment output;

                float4 posWS = mul(UNITY_MATRIX_M, float4(input.vertex.xyz, 1.0));
                float4 posVS = mul(UNITY_MATRIX_V, float4(posWS.xyz, 1.0));
                float4 posPS = mul(UNITY_MATRIX_P, float4(posVS.xyz, 1.0));

                output.pos = posPS;
                output.posVS = posVS;

                return output;
            }

            float4 LinearizedFragment(VertexToFragment input) : SV_Target
            {
                float linearizedPosZ = -input.posVS.z;
                float linearizedDepth = (linearizedPosZ - _clipPlane.x) / _clipPlane.y;

                return float4(linearizedDepth, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }*/
        //seongdae
    }
    Dependency "AddPassShader" = "Hidden/LightweightPipeline/Terrain/Standard Terrain Add Pass"
    Dependency "BaseMapShader" = "Hidden/LightweightPipeline/Terrain/Standard Terrain Base"

    Fallback "Hidden/InternalErrorShader"
}
