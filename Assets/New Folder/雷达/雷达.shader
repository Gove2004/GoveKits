Shader "Custom/SonarScan_Perfect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Radar Settings)]
        [HDR] _ScanColor ("Scan Color", Color) = (0, 1, 0, 1)
        _ScanSpeed ("Speed", Range(0, 10)) = 3.0
        _Width ("Wave Width", Range(0, 0.2)) = 0.05
        
        [Header(Fix Shape)]
        _Aspect ("Aspect Ratio (Width/Height)", Float) = 1.0 // 长宽比修正
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One // 叠加发光模式

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            fixed4 _ScanColor;
            float _ScanSpeed;
            float _Width;
            float _Aspect;

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
                // 1. 修正中心点坐标
                float2 uv = i.uv - 0.5;
                
                // 2. 【核心修复】长宽比修正
                // 如果图片是宽的 (Aspect > 1)，我们把 X 轴坐标乘大，让计算出的距离变大
                // 这样画出来的圆在UV空间是扁的，但在屏幕上就是圆的了
                uv.x *= _Aspect;

                // 3. 计算距离 (现在是正圆距离了)
                float dist = length(uv);
                
                // 4. 生成波纹 (向外扩散)
                // frac 让值在 0~1 之间循环
                float waveProgress = frac(_Time.y * _ScanSpeed * 0.2);
                
                // 计算当前像素是否在波纹环内
                // abs(dist - waveProgress) < _Width
                float beam = 1.0 - smoothstep(_Width, _Width + 0.01, abs(dist - waveProgress));

                // 5. 【新增】边缘防穿帮虚化
                // 算出离图片边缘最近的距离
                // 原始 UV 是 0~1。离中心的距离最大是 0.5
                // 如果修正了长宽比，边缘判定比较麻烦。
                // 简单做法：利用原始 i.uv 算一个方形遮罩
                float2 borderDist = abs(i.uv - 0.5);
                // 只要接近 0.5 (边缘)，就变透明
                float maskX = 1.0 - smoothstep(0.4, 0.5, borderDist.x);
                float maskY = 1.0 - smoothstep(0.4, 0.5, borderDist.y);
                float edgeFade = maskX * maskY;

                // 6. 最终合成
                // 原图 + (扫描色 * 波纹强度 * 边缘遮罩)
                fixed4 texCol = tex2D(_MainTex, i.uv) * i.color;
                
                return texCol + _ScanColor * beam * edgeFade;
            }
            ENDCG
        }
        
    }
}