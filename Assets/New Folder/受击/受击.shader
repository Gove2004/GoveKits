Shader "Custom/HitFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Hit Settings)]
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0 // 0=正常，1=全白
        [HDR] _FlashColor ("Flash Color", Color) = (1,1,1,1) // 默认纯白，也可以是受击红
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float _FlashAmount;
            fixed4 _FlashColor;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                c.rgb *= c.a; // 预乘 Alpha

                // 核心逻辑：
                // 1. 算出闪白后的颜色 (直接用 FlashColor)
                // 2. 混合：lerp(原色, 闪光色, 进度)
                // 注意：我们只改变 RGB，Alpha 保持不变
                
                fixed3 finalRGB = lerp(c.rgb * i.color.rgb, _FlashColor.rgb * c.a, _FlashAmount);
                
                return fixed4(finalRGB, c.a * i.color.a);
            }
            ENDCG
        }
    }
}