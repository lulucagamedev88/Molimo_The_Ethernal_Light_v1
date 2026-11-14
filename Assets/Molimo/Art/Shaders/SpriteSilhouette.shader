Shader "Molimo/SpriteSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" { }
_Color("Tint", Color) = (1, 1, 1, 1)
[HideInInspector] _RendererColor("RendererColor", Color) = (1, 1, 1, 1)
[HideInInspector] _Flip("Flip", Vector) = (1, 1, 1, 1)
[PerRendererData] _AlphaTex("External Alpha", 2D) = "white" { }
[PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

[Header(Silhouette)]
        _SilhouetteColor("Silhouette Color", Color) = (1, 1, 1, 1)
        _SilhouetteAmount("Silhouette Amount", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteSilhouette"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
#pragma fragment frag
#pragma target 2.0
#pragma multi_compile _ ETC1_EXTERNAL_ALPHA

# include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
{
    float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 texcoord   : TEXCOORD0;
            };

struct Varyings
{
    float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 texcoord   : TEXCOORD0;
            };

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);

CBUFFER_START(UnityPerMaterial)
                float4 _Color;
float4 _RendererColor;
float4 _Flip;
float _EnableExternalAlpha;
float4 _SilhouetteColor;
float _SilhouetteAmount;
CBUFFER_END

Varyings vert(Attributes IN)
            {
                Varyings OUT;
IN.positionOS.xy *= _Flip.xy;
OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
OUT.texcoord = IN.texcoord;
OUT.color = IN.color * _Color * _RendererColor;
return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.texcoord) * IN.color;

# ifdef ETC1_EXTERNAL_ALPHA
half4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, IN.texcoord);
c.a = lerp(c.a, alpha.a, _EnableExternalAlpha);
#endif

// Silhouette: preserva o alpha (forma), força a cor
half4 sil = _SilhouetteColor;
sil.a = c.a;
c = lerp(c, sil, _SilhouetteAmount);

return c;
            }
            ENDHLSL
        }
    }
}