Shader "Molimo/UI/HealthVeinVignette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Vignette Hole)]
        _HoleRadius   ("Hole Radius - 0 closed, 1 open", Range(0, 1))   = 1
        _Feather      ("Feather", Range(0, 0.5))                        = 0.08
        _HoleAspect   ("Hole Aspect", Float)                            = 1.0

        [Header(Pulse)]
        _PulseIntensity ("Pulse Intensity", Range(0, 2))                = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Color;
            float     _HoleRadius;
            float     _Feather;
            float     _HoleAspect;
            float     _PulseIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv) * i.color;

                // SDF do oval 16:9
                float2 c = i.uv - 0.5;
                c.x *= _HoleAspect;
                float d = length(c);

                float r = _HoleRadius * 0.5;
                float mask = smoothstep(r - _Feather, r + _Feather, d);

                float alpha = tex.a * mask * _PulseIntensity;
                float3 rgb  = tex.rgb * (1.0 + (_PulseIntensity - 1.0) * 0.4);

                return float4(rgb, saturate(alpha));
            }
            ENDHLSL
        }
    }
    Fallback "UI/Default"
}