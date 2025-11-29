Shader "Custom/HalftoneComic"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _DotSize ("Dot Density", Range(10, 200)) = 80 // 网点密度
        _Smoothness ("Dot Softness", Range(0.01, 0.5)) = 0.1 // 边缘硬度
        _PaperColor ("Paper Color", Color) = (0.1, 0.1, 0.1, 1) // 阴影底色
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
            float _DotSize;
            float _Smoothness;
            fixed4 _PaperColor;

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

                // 1. 计算亮度 (Luminance)
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));

                // 2. 生成屏幕空间的网格
                // i.screenPos.xy / i.screenPos.w 得到 0-1 的屏幕坐标
                // 乘以 _Aspect 是为了保证圆点是圆的不是扁的(假设宽高比1.77)
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                screenUV.x *= 1.777; 
                
                // sin 函数生成网格波形 (-1 到 1)
                float2 grid = sin(screenUV * _DotSize * 3.14);
                
                // 组合 XY 的波形，得到网点形状
                // 这里的算式构建了一个 grid pattern
                float dots = (grid.x * grid.y) * 0.5 + 0.5;

                // 3. 核心比较
                // 如果当前点的“网点值” < “亮度”，则显示原色
                // 否则显示底色
                // smoothstep 用于抗锯齿
                float pattern = smoothstep(lum - _Smoothness, lum + _Smoothness, dots);

                // pattern = 0 (亮部), pattern = 1 (暗部)
                // 亮部显示原图，暗部显示纸张色(或者变黑)
                col.rgb = lerp(col.rgb, _PaperColor.rgb, pattern);

                return col;
            }
            ENDCG
        }
    }
}