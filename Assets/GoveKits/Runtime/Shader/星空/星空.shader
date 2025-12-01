Shader "Custom/GalaxyPortal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Galaxy)]
        _BaseColor ("Deep Space Color", Color) = (0.1, 0, 0.2, 1) // 深紫背景
        _StarColor ("Star Color", Color) = (1, 1, 1, 1)
        _Density ("Star Density", Float) = 5.0
        _Speed ("Parallax Speed", Float) = 0.1
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
                float4 screenPos : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            fixed4 _BaseColor;
            fixed4 _StarColor;
            float _Density;
            float _Speed;

            // 伪随机
            float rand(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // 绘制一层星星
            float StarLayer(float2 uv, float scale, float speedOffset) {
                uv *= scale; // 缩放
                uv += float2(_Time.y * speedOffset, _Time.y * speedOffset * 0.5); // 移动
                
                float2 grid = floor(uv);
                float2 pos = frac(uv);
                
                float star = 0;
                
                // 在每个格子里随机生成星星
                float r = rand(grid);
                
                // 只有随机值 > 0.95 才生成星星 (稀疏)
                if (r > 0.95) {
                    // 星星位置稍微随机偏一点
                    float2 center = float2(0.5, 0.5) + (rand(grid + 1.0) - 0.5) * 0.5;
                    float dist = distance(pos, center);
                    
                    // 画圆，带光晕
                    // 0.05 是核心大小， 0.4 是光晕范围
                    star = smoothstep(0.4, 0.05, dist); 
                    
                    // 让星星随机闪烁
                    star *= 0.5 + 0.5 * sin(_Time.y * 5.0 + r * 10.0);
                }
                return star;
            }

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mainCol = tex2D(_MainTex, i.uv);
                if (mainCol.a < 0.1) discard;

                // 使用屏幕坐标，制造视差
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // --- 绘制 3 层星星 ---
                // 第一层：大星星，动得快，密度低
                float stars1 = StarLayer(screenUV, _Density * 5.0, _Speed * 1.0);
                
                // 第二层：中星星，动得慢，密度中
                float stars2 = StarLayer(screenUV, _Density * 10.0, _Speed * 0.5);
                
                // 第三层：小星星，几乎不动，密度高 (远景)
                float stars3 = StarLayer(screenUV, _Density * 20.0, _Speed * 0.2);

                float totalStars = stars1 + stars2 + stars3;
                
                // --- 颜色合成 ---
                // 背景色 + 星星色
                fixed4 finalCol = _BaseColor + _StarColor * totalStars;
                
                // 乘上原图的 Alpha，保证形状正确
                finalCol.a = mainCol.a;
                // 乘上原图的明暗 (可选，如果想保留原图纹理细节)
                // finalCol.rgb *= mainCol.rgb; 

                return finalCol * i.color;
            }
            ENDCG
        }
    }
}