Shader "SliceyMesh/UnlitTexture"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)

        [Header(BLENDING)]

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Int) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlpha("Source Blend Alpha", Int) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dest Blend Mode", Int) = 10 // OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlpha("Dest Blend Alpha", Int) = 10 // OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("BlendOp", Int) = 0 // Add
        [Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorWriteMask("Color Write Mask", Int) = 15
        [Toggle] _AlphaToMask("Alpha To Mask", Int) = 0
        
        [Header(CULLING)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", int) = 2 // Back face culling
        [Toggle] _ZWrite("ZWrite?", Int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Z Test", int) = 4

        [Header(STENCIL)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Int) = 8
        _Stencil("Stencil ID", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)]_StencilOp("Stencil Operation", Int) = 0
        _StencilWriteMask("Stencil Write Mask", Int) = 255
        _StencilReadMask("Stencil Read Mask", Int) = 255
    }
    SubShader
    {
        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        BlendOp[_BlendOp]
        Blend[_SrcBlend][_DstBlend],[_SrcBlendAlpha][_DstBlendAlpha]
        ColorMask[_ColorWriteMask]
        AlphaToMask[_AlphaToMask]
        ZWrite[_ZWrite]
        ZTest[_ZTest]
        Cull[_CullMode]

        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 uv = v.vertex.xy;
                o.uv = TRANSFORM_TEX(uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
