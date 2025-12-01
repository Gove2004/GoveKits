Shader "Custom/GodModeShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Shadow Settings)]
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.5)
        _ShadowOffset ("Offset (X, Y)", Vector) = (0, -0.1, 0, 0) // 影子的位移
        
        [Header(Canvas)]
        _CanvasScale ("Canvas Scale", Range(1.0, 2.0)) = 1.3 // 画布放大倍数
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            
            fixed4 _ShadowColor;
            float4 _ShadowOffset;
            float _CanvasScale;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                // 1. 撑大模型，给影子留位置
                float4 expandedPos = IN.vertex;
                expandedPos.xy *= _CanvasScale;
                
                OUT.vertex = UnityObjectToClipPos(expandedPos);
                
                // 2. 修正 UV，保证图片居中
                float2 center = float2(0.5, 0.5);
                OUT.texcoord = (IN.texcoord - center) * _CanvasScale + center;
                
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // 1. 采样本体
                fixed4 mainCol = fixed4(0,0,0,0);
                // 只有在合法的 UV 范围内才采样，否则是透明的
                if(uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1)
                {
                    mainCol = tex2D(_MainTex, uv) * IN.color;
                }

                // 2. 采样影子
                // 影子的 UV = 当前 UV - 偏移量
                float2 shadowUV = uv - _ShadowOffset.xy;
                fixed4 shadowCol = fixed4(0,0,0,0);
                
                if(shadowUV.x >= 0 && shadowUV.x <= 1 && shadowUV.y >= 0 && shadowUV.y <= 1)
                {
                    // 只取 Alpha 通道，颜色强制用 ShadowColor
                    fixed alpha = tex2D(_MainTex, shadowUV).a;
                    shadowCol = _ShadowColor;
                    shadowCol.a *= alpha;
                }

                // 3. 混合 (Blend)
                // 经典公式：本体盖在影子上
                // 最终颜色 = 本体 * 本体A + 影子 * (1 - 本体A)
                fixed4 finalCol = mainCol + shadowCol * (1.0 - mainCol.a);
                
                return finalCol;
            }
            ENDCG
        }
    }
}