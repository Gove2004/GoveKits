Shader "Custom/StainedGlass_Crystal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _CellSize ("Crystal Size", Float) = 20.0 // 晶格大小
        _BorderWidth ("Border Width", Range(0, 0.2)) = 0.05 // 玻璃黑边
        _BorderColor ("Border Color", Color) = (0,0,0,1)
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
                fixed4 color    : COLOR;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float _CellSize;
            float _BorderWidth;
            fixed4 _BorderColor;

            // 伪随机
            float2 rand2(float2 p) {
                return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);
            }

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
                float2 uv = i.uv;
                float2 st = uv * _CellSize;
                float2 i_st = floor(st);
                float2 f_st = frac(st);

                float m_dist = 1.0;
                float2 cell_center;  // 最终确定的晶格中心 UV

                // Voronoi 寻找最近点
                for (int y= -1; y <= 1; y++) {
                    for (int x= -1; x <= 1; x++) {
                        float2 neighbor = float2(float(x),float(y));
                        
                        // 【修复点】变量名从 point 改为 rndPoint
                        float2 rndPoint = rand2(i_st + neighbor);
                        
                        // 让点微微动一下，增加灵动感
                        rndPoint = 0.5 + 0.5 * sin(_Time.y * 0.5 + 6.2831 * rndPoint);
                        
                        float2 diff = neighbor + rndPoint - f_st;
                        float dist = length(diff);

                        if(dist < m_dist) {
                            m_dist = dist;
                            // 计算出那个特征点在全图的 UV 坐标
                            // (当前格子ID + 邻居偏移 + 特征点局部坐标) / 总格子数
                            cell_center = (i_st + neighbor + rndPoint) / _CellSize;
                        }
                    }
                }

                // --- 核心魔法：去采中心的颜色 ---
                // 我们不采 i.uv，而是采 cell_center
                fixed4 crystalCol = tex2D(_MainTex, cell_center);
                
                // 保持原图的透明度
                fixed4 originCol = tex2D(_MainTex, i.uv);
                crystalCol.a = originCol.a;

                // --- 增加立体感 ---
                // 根据距离中心的远近调整亮度，让色块看起来像凸起的宝石
                crystalCol.rgb += (0.5 - m_dist) * 0.3;

                return crystalCol * i.color;
            }
            ENDCG
        }
    }
}