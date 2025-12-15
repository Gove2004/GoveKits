Shader "Custom/LiquidBlock"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Liquid Color", Color) = (0, 0.7, 1, 1) // 水的颜色
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)   // 表面泡沫颜色
        
        _FillAmount ("Fill Amount", Range(0, 1)) = 0.6    // 水位高度
        _WaveSpeed ("Wave Speed", Range(0, 10)) = 2       // 波浪速度
        _WaveAmp ("Wave Amplitude", Range(0, 0.1)) = 0.02 // 波浪起伏高度
        _FoamWidth ("Foam Width", Range(0, 0.05)) = 0.02  // 泡沫线条粗细
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
                float4 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _FoamColor;
            float _FillAmount;
            float _WaveSpeed;
            float _WaveAmp;
            float _FoamWidth;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. 计算当前的波浪高度
                // 使用世界坐标 X 轴，这样并排的水面能连起来
                float waveHeight = sin(i.worldPos.x * 5 + _Time.y * _WaveSpeed) * _WaveAmp;
                
                // 实际的水位线 = 基础水位 + 波浪偏移
                float currentLevel = _FillAmount + waveHeight;

                // 2. 判断当前像素在哪里
                // 如果 UV.y 高于水位线 -> 透明（空气）
                // 如果 UV.y 低于水位线 -> 水
                
                float diff = currentLevel - i.uv.y;

                if (diff < 0) 
                {
                    discard; // 空气
                }

                fixed4 finalColor = _Color;

                // 3. 画泡沫
                // 如果距离水位线很近 (diff 很小)，就显示泡沫色
                if (diff < _FoamWidth)
                {
                    finalColor = _FoamColor;
                }

                // 4. 增加一点通透感 (可选)
                // 让下面的水稍微深一点
                finalColor.rgb *= (1.0 - (1.0 - i.uv.y) * 0.3);

                return finalColor;
            }
            ENDCG
        }
    }
}