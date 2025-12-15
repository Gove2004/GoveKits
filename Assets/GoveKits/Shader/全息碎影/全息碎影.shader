Shader "Custom/HoloGlitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _ScanSpeed ("Scan Speed", Range(0, 10)) = 2 // 扫描线速度
        _GlitchAmount ("Glitch Shake", Range(0, 0.1)) = 0.02 // 抖动程度
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha // 这种混合模式会让颜色有点发光感

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 screenPos : TEXCOORD1; // 屏幕坐标用来做扫描线
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float _ScanSpeed;
            float _GlitchAmount;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                
                // --- 顶点抖动 (Glitch) ---
                // 利用时间做随机数，偶尔把顶点往左右扯一下
                float time = _Time.y * 10;
                float noise = sin(time) * sin(time * 3.14); // 简单的伪随机
                
                // 只有当 noise 大于某个值时才抖动（模拟接触不良）
                float shake = (noise > 0.9) ? _GlitchAmount : 0;
                
                float4 v = IN.vertex;
                v.x += shake; // 抖动 X 轴
                
                OUT.vertex = UnityObjectToClipPos(v);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. 扫描线效果
                // 利用屏幕坐标的 Y 轴 + 时间，通过 sin 函数做出条纹
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float scanLine = sin(screenUV.y * 50 + _Time.y * _ScanSpeed);
                
                // 把 -1到1 的 sin 波变成了 0.5 到 1 的亮度条纹
                // 也就是：有的地方亮，有的地方暗，像旧电视
                scanLine = scanLine * 0.2 + 0.8; 

                // 2. 颜色偏移 (色差效果 Chromatic Aberration)
                // 稍微错开一点点去采样 R 和 B 通道
                float2 uv = IN.texcoord;
                fixed4 c;
                c.r = tex2D(_MainTex, uv + float2(0.005, 0)).r; // 红色偏左
                c.g = tex2D(_MainTex, uv).g;                    // 绿色不动
                c.b = tex2D(_MainTex, uv - float2(0.005, 0)).b; // 蓝色偏右
                c.a = tex2D(_MainTex, uv).a;

                // 3. 变成全息蓝
                // 把原图颜色稍微盖掉一点，染成青蓝色
                fixed4 holoColor = fixed4(0, 1, 1, 1);
                c = lerp(c, holoColor, 0.3); // 30% 染成全息色

                // 叠加扫描线亮度
                c.rgb *= scanLine;
                // 增加一点整体透明度波动
                c.a *= (0.9 + sin(_Time.y * 20) * 0.1);

                return c * IN.color;
            }
            ENDCG
        }
    }
}