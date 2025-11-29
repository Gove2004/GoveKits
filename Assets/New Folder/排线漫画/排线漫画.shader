Shader "Custom/MangaSketchHatch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _Density ("Line Density", Float) = 80.0 // 线条密度
        _LineWidth ("Line Width", Range(0, 1)) = 0.5 // 基础粗细
        _PaperColor ("Paper Color", Color) = (1, 0.95, 0.9, 1) // 米黄色纸张
        _InkColor ("Ink Color", Color) = (0.1, 0.1, 0.1, 1) // 墨水黑
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
            float _Density;
            float _LineWidth;
            fixed4 _PaperColor;
            fixed4 _InkColor;

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
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                if (col.a < 0.1) discard;

                // 1. 计算亮度
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                
                // 2. 屏幕空间 UV (保证线条不随物体旋转而旋转，像贴在屏幕上)
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                screenUV.x *= 1.777; // 修正长宽比

                // 3. 生成排线图案
                // 线条 1: 45度 (/ 方向)
                float line1 = sin((screenUV.x + screenUV.y) * _Density * 3.14);
                
                // 线条 2: 135度 (\ 方向)
                float line2 = sin((screenUV.x - screenUV.y) * _Density * 3.14);

                // 4. 动态线宽
                // 亮度越低(越暗)，线条应该越粗(阈值越高)
                // 映射亮度到 -1 ~ 1 之间作为阈值
                float threshold = (lum - 0.5) * 2.0 + (1.0 - _LineWidth);

                // 5. 判定是否画线
                // 如果 pattern 值 < 阈值，则是墨水
                float hatch1 = step(threshold, line1);
                float hatch2 = step(threshold, line2);
                
                // 稍微亮一点的地方只有单层线，很暗的地方有双层线
                // 混合逻辑：越暗，threshold 越小，越容易触发 step 返回 0 (墨水)
                
                // 这里用简单的逻辑：
                // 极亮：白
                // 中间：单线
                // 暗：双线交叉
                
                float hatch = 1.0;
                
                if (lum > 0.8) hatch = 1.0; // 纯白
                else if (lum > 0.5) hatch = hatch1; // 单线
                else hatch = min(hatch1, hatch2); // 交叉线 (取最小值即取黑色多的)

                // 6. 上色
                fixed4 finalCol = lerp(_InkColor, _PaperColor, hatch);
                finalCol.a = col.a;

                return finalCol;
            }
            ENDCG
        }
    }
}