Shader "SliceyMesh/UnlitTextureTriplanar"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _TriplanarBlendSharpness("Triplanar Blend Sharpness", float) = 5
        [Toggle(MIRROR)] _Mirror ("Mirror", Int) = 0

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
            #pragma shader_feature _ MIRROR

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv_x : TEXCOORD0;
                float2 uv_y : TEXCOORD1;
                float2 uv_z : TEXCOORD2;
                half3 uv_weights : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                float4 position : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _TriplanarBlendSharpness;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.position);
                o.uv_x = TRANSFORM_TEX(v.position.zy, _MainTex);
                o.uv_y = TRANSFORM_TEX(v.position.xz, _MainTex);
                o.uv_z = TRANSFORM_TEX(v.position.xy, _MainTex);

#if MIRROR
                if (v.normal.x < 0)
                {
                    o.uv_x.x = -o.uv_x.x;
                }
                if (v.normal.y < 0)
                {
                    o.uv_y.x = -o.uv_y.x;
                }
                if (v.normal.z < 0)
                {
                    o.uv_z.x = -o.uv_z.x;
                }
#endif

                o.uv_weights = pow(abs(v.normal), _TriplanarBlendSharpness);
                UNITY_TRANSFER_FOG(o, o.position);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 tex_x = tex2D(_MainTex, i.uv_x);
                fixed4 tex_y = tex2D(_MainTex, i.uv_y);
                fixed4 tex_z = tex2D(_MainTex, i.uv_z);

                half3 blendWeights = i.uv_weights / (i.uv_weights.x + i.uv_weights.y + i.uv_weights.z);
                fixed4 col = tex_x * blendWeights.x + tex_y * blendWeights.y + tex_z * blendWeights.z;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
