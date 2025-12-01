Shader "Custom/UltimateVectorArt"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Vector Quality)]
        _FilterRadius ("Smoothing Radius", Range(0, 10)) = 4 // 涂抹范围(色块大小)
        _EdgeSens ("Edge Sensitivity", Range(0.01, 0.5)) = 0.1 // 边缘敏感度(越小边缘越碎)
        
        [Header(Color Style)]
        _Posterize ("Color Steps", Range(2, 20)) = 6 // 色阶数量
        _Saturation ("Saturation Boost", Range(1, 2)) = 1.2 // 矢量图通常色彩鲜艳
        
        [Header(Atmosphere)]
        _GradientStr ("Gradient Overlay", Range(0, 0.5)) = 0.1 // 模拟矢量插画的渐变感
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
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 texelSize : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            float _FilterRadius;
            float _EdgeSens;
            float _Posterize;
            float _Saturation;
            float _GradientStr;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                o.texelSize = _MainTex_TexelSize;
                return o;
            }

            fixed3 AdjustSaturation(fixed3 color, float saturation)
            {
                float grey = dot(color, float3(0.2126, 0.7152, 0.0722));
                return lerp(grey, color, saturation);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // --- 1. 智能表面模糊 (Smart Surface Blur) ---
                // 这是一个简化版的双边滤波
                
                float4 sumColor = float4(0,0,0,0);
                float totalWeight = 0.0;
                
                // 获取中心点颜色
                float4 centerCol = tex2D(_MainTex, uv);
                
                int r = floor(_FilterRadius);
                
                // 循环采样周围像素
                for (int x = -r; x <= r; x++) 
                {
                    for (int y = -r; y <= r; y++) 
                    {
                        // 采样
                        float2 offset = float2(x, y) * i.texelSize.xy;
                        float4 sampleCol = tex2D(_MainTex, uv + offset);
                        
                        // 计算颜色差异 (欧氏距离)
                        float diff = distance(centerCol.rgb, sampleCol.rgb);
                        
                        // --- 核心魔法 ---
                        // 计算权重：颜色差异越小，权重越大；差异越大，权重越接近0
                        // 这样就只平滑了内部，而不会模糊边缘
                        float weight = 1.0 - smoothstep(0, _EdgeSens, diff);
                        
                        // 空间距离权重 (高斯分布，可选，为了性能可忽略)
                        // float spaceWeight = 1.0 - length(float2(x,y)) / float(r);
                        
                        sumColor += sampleCol * weight;
                        totalWeight += weight;
                    }
                }
                
                // 得到平滑后的“矢量色”
                float4 finalCol = sumColor / (totalWeight + 0.001); // 防止除0

                // --- 2. 色阶量化 (Quantization) ---
                // 让颜色断层，形成插画感
                finalCol.rgb = floor(finalCol.rgb * _Posterize) / _Posterize;
                
                // 稍微做一点平滑过渡，防止色阶边缘锯齿太严重
                // (可选操作，矢量图通常喜欢硬边)
                
                // --- 3. 饱和度增强 ---
                // 矢量插画通常颜色很纯
                finalCol.rgb = AdjustSaturation(finalCol.rgb, _Saturation);

                // --- 4. 垂直渐变叠加 (Atmosphere) ---
                // 很多矢量风景画都有这种从上到下的微弱渐变
                float gradient = (i.uv.y - 0.5) * _GradientStr;
                finalCol.rgb += gradient;

                // 保持原图透明度边缘
                finalCol.a = centerCol.a;
                if (finalCol.a < 0.1) discard;

                return finalCol * i.color;
            }
            ENDCG
        }
    }
}