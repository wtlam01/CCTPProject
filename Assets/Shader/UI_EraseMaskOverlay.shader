Shader "UI/EraseMaskOverlay"
{
    Properties
    {
        _Color("Overlay Color", Color) = (1,0.5,0,1)
        _MaskTex("Mask (R)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };

            fixed4 _Color;
            sampler2D _MaskTex;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed m = tex2D(_MaskTex, i.uv).r; // 1=keep overlay, 0=erased
                fixed4 col = _Color;
                col.a *= m;
                return col;
            }
            ENDCG
        }
    }
}