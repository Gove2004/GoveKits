Shader "Custom/RainyWindow_Procedural_Final"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(No Texture Needed)]
        _RainColor ("Rain Color", Color) = (0.8, 0.9, 1, 1) // 雨滴颜色(偏蓝白)
        _Size ("Size / Density", Float) = 10.0 // 网格密度(越大雨越密)
        _Speed ("Speed", Float) = 1.0          // 下落速度
        _Distortion ("Distortion", Range(0, 1)) = 0.5 // 扭曲程度
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
            };

            fixed4 _Color;
            sampler2D _MainTex;
            fixed4 _RainColor;
            float _Size;
            float _Speed;
            float _Distortion;

            // --- 伪随机函数 (生成杂色) ---
            float N21(float2 p) {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            // --- 核心：绘制一层雨滴 ---
            float Layer(float2 uv, float t) {
                // 1. 制作网格
                float2 aspect = float2(2, 1); // 拉长Y轴，让格子变长方形
                float2 uv2 = uv * _Size * aspect;
                uv2.y += t * 0.25; // 向下移动
                
                float2 gv = frac(uv2) - 0.5; // 格子内坐标 (-0.5 ~ 0.5)
                float2 id = floor(uv2);      // 格子ID (用于随机)

                // 2. 给每个格子生成一个随机的雨滴偏移量
                float n = N21(id); // 0~1的随机数
                t += n * 6.2831;   // 随机时间偏移

                // 3. 计算雨滴位置
                // x: 在格子内左右随机摆动一点
                // y: 这是一个锯齿波，让雨滴周期性落下
                float w = uv.y * 10.0;
                float x = (n - 0.5) * 0.8; 
                x += (0.4 - abs(x)) * sin(3.0 * w) * pow(sin(w), 6.0) * 0.45;
                
                float y = -sin(t + sin(t + sin(t) * 0.5)) * 0.45;
                // 把雨滴形状变瘦长，模拟下落
                y -= (gv.x - x) * (gv.x - x); 

                // 4. 计算雨滴形状 (SDF)
                float2 dropPos = (gv - float2(x, y));
                float drop = length(dropPos);

                // 5. 绘制拖尾
                // 如果当前点在雨滴上方，且距离很近，就是拖尾
                float trail = smoothstep(0.05, 0.03, drop); // 雨滴本体
                // 这里加一点拖尾的逻辑...简化版直接用本体
                
                // 只要在圆圈内就是 1
                return smoothstep(0.06, 0.03, drop); 
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;
                
                // --- 1. 计算两层雨滴 (制造视差) ---
                float drops = 0;
                drops += Layer(i.uv, t); // 第一层
                drops += Layer(i.uv * 1.23 + 5.54, t); // 第二层(错位)
                
                // --- 2. 扭曲 UV ---
                float2 distortOffset = float2(drops, drops) * _Distortion * 0.1;
                fixed4 col = tex2D(_MainTex, i.uv + distortOffset) * i.color;

                // --- 3. 叠加白色雨滴 (关键！) ---
                // 不管背景是什么颜色，只要有雨滴，就叠加一层白色
                // 使用 max 或者 lerp 都可以
                // 这里我们让雨滴有点半透明
                col.rgb = lerp(col.rgb, _RainColor.rgb, drops * _RainColor.a);
                
                // 稍微增加一点高光
                col.rgb += drops * 0.2;

                return col;
            }
            ENDCG
        }
    }
}