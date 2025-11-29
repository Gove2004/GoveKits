Shader "Custom/LiquidHealthBar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Back Color", Color) = (0.2, 0, 0, 1) // 空血时的背景色
        
        [Header(Liquid)]
        _FillAmount ("Fill Amount", Range(0, 1)) = 0.5
        _LiquidColor ("Front Color", Color) = (1, 0.2, 0.2, 1) // 前浪颜色(鲜红)
        _BackLiquidColor ("Back Color", Color) = (0.6, 0.1, 0.1, 1) // 后浪颜色(深红)
        
        _WaveSpeed ("Wave Speed", Float) = 5.0
        _WaveAmp ("Wave Amplitude", Float) = 0.05
        _RimColor ("Glass Rim", Color) = (1, 1, 1, 0.5) // 玻璃管高光
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
            };

            fixed4 _Color;
            float _FillAmount;
            fixed4 _LiquidColor;
            fixed4 _BackLiquidColor;
            float _WaveSpeed;
            float _WaveAmp;
            fixed4 _RimColor;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. 基础背景
                fixed4 finalCol = _Color;

                // 2. 计算后浪 (Back Wave)
                // 相位偏移一些，看起来有层次
                float waveBack = sin(i.uv.x * 10.0 + _Time.y * _WaveSpeed * 0.8) * _WaveAmp;
                float levelBack = _FillAmount + waveBack;
                
                if (i.uv.y < levelBack)
                {
                    finalCol = _BackLiquidColor;
                }

                // 3. 计算前浪 (Front Wave)
                // 频率稍微不同，产生交错感
                float waveFront = sin(i.uv.x * 12.0 + _Time.y * _WaveSpeed) * _WaveAmp;
                float levelFront = _FillAmount + waveFront;

                if (i.uv.y < levelFront)
                {
                    finalCol = _LiquidColor;
                    
                    // 给前浪加一点顶部高光 (表面张力)
                    if (i.uv.y > levelFront - 0.05)
                        finalCol += fixed4(0.2, 0.2, 0.2, 0);
                }

                // 4. 玻璃管高光 (Rim)
                // 简单的两边亮，中间暗，模拟圆柱体反光
                float rim = pow(abs(i.uv.y - 0.5) * 2.0, 3.0); 
                // 只在边缘加一点点白
                finalCol.rgb += _RimColor.rgb * rim * 0.3;
                
                // 顶部和底部也加一点亮边
                float vRim = pow(abs(i.uv.x - 0.5) * 2.0, 10.0);
                finalCol.rgb += _RimColor.rgb * vRim * 0.5;

                return finalCol;
            }
            ENDCG
        }
    }
}