Shader "Custom/GameboyStyle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // 经典的 GB 四色 (从最暗到最亮)
        _Color1 ("Darkest", Color) = (0.06, 0.22, 0.06, 1) // 深黑绿
        _Color2 ("Dark", Color) = (0.19, 0.38, 0.19, 1)    // 暗绿
        _Color3 ("Light", Color) = (0.55, 0.67, 0.06, 1)   // 亮绿
        _Color4 ("Lightest", Color) = (0.61, 0.73, 0.06, 1) // 最亮绿
        
        _DitherSize ("Dither Size", Float) = 1.0 // 像素颗粒大小
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
                float2 uv       : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            fixed4 _Color1, _Color2, _Color3, _Color4;
            float _DitherSize;

            // 4x4 拜耳矩阵 (Bayer Matrix)
            // 用来决定“在这个亮度下，这个像素该不该亮”
            static const float4x4 bayer = float4x4(
                0,  8,  2, 10,
                12, 4, 14,  6,
                3, 11,  1,  9,
                15, 7, 13,  5
            ) / 16.0;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                if (col.a < 0.1) discard;

                // 1. 计算亮度
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));

                // 2. 获取屏幕空间的像素坐标 (用于对齐矩阵)
                float2 screenPos = i.screenPos.xy / i.screenPos.w;
                // _ScreenParams.xy 是屏幕分辨率
                // 乘以 DitherSize 来控制颗粒感
                int2 ditherUV = int2(screenPos * _ScreenParams.xy / _DitherSize);
                
                // 3. 从矩阵中取值
                // 使用取模 (%) 运算，让矩阵在全屏平铺
                float threshold = bayer[ditherUV.x % 4][ditherUV.y % 4];

                // 4. 核心逻辑：抖动量化
                // 把亮度分为 4 个层级。
                // 加上 threshold 是为了让处于临界值的像素产生“疏密变化”
                
                float level = lum + (threshold - 0.5) * 0.5; // 0.5是抖动强度
                
                fixed3 finalRGB;
                
                if (level < 0.25) finalRGB = _Color1.rgb;
                else if (level < 0.5) finalRGB = _Color2.rgb;
                else if (level < 0.75) finalRGB = _Color3.rgb;
                else finalRGB = _Color4.rgb;

                return fixed4(finalRGB, col.a);
            }
            ENDCG
        }
    }
}