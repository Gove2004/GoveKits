Shader "Custom/OilPaint_Kuwahara"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Radius ("Brush Size", Range(1, 10)) = 4 // 笔触大小(半径)
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
                float4 texelSize : TEXCOORD1; // 纹理像素大小
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // Unity 自动赋值: (1/w, 1/h, w, h)
            int _Radius;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.texelSize = _MainTex_TexelSize;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                int radius = _Radius;
                float2 uv = i.uv;
                float2 texel = i.texelSize.xy;

                // 定义 4 个区域的 均值(m) 和 方差(s)
                float3 m[4];
                float3 s[4];
                for (int k = 0; k < 4; ++k) {
                    m[k] = float3(0, 0, 0);
                    s[k] = float3(0, 0, 0);
                }

                // 暴力循环 4 个象限 (这是一个 O(N^2) 的算法，半径别开太大！)
                // 区域 0: 左上 (-r, -r) 到 (0, 0)
                // 区域 1: 右上 (0, -r) 到 (r, 0) ... 以此类推
                
                int samples = (radius + 1) * (radius + 1);

                // 为了代码简洁，这里展开写循环逻辑稍微有点复杂
                // 我们用最通用的双重循环 + if 判断落入哪个区域
                
                for (int y = -radius; y <= radius; ++y) {
                    for (int x = -radius; x <= radius; ++x) {
                        fixed4 col = tex2D(_MainTex, uv + float2(x, y) * texel);
                        float3 c = col.rgb;
                        
                        // 左上
                        if (x <= 0 && y <= 0) { m[0] += c; s[0] += c * c; }
                        // 右上
                        if (x >= 0 && y <= 0) { m[1] += c; s[1] += c * c; }
                        // 左下
                        if (x <= 0 && y >= 0) { m[2] += c; s[2] += c * c; }
                        // 右下
                        if (x >= 0 && y >= 0) { m[3] += c; s[3] += c * c; }
                    }
                }

                float minSigma2 = 1e+2;
                float3 finalCol = float3(0, 0, 0);
                
                // 计算方差并找出最小的
                for (int k = 0; k < 4; ++k) {
                    m[k] /= samples;
                    s[k] = abs(s[k] / samples - m[k] * m[k]);

                    float sigma2 = s[k].r + s[k].g + s[k].b;
                    if (sigma2 < minSigma2) {
                        minSigma2 = sigma2;
                        finalCol = m[k];
                    }
                }
                
                // 保持原图 alpha
                fixed originalAlpha = tex2D(_MainTex, uv).a;
                return fixed4(finalCol, originalAlpha);
            }
            ENDCG
        }
    }
}