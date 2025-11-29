Shader "Custom/DiamondSparkle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [HDR] _SparkleColor ("Sparkle Color", Color) = (1, 1, 1, 1) // 闪光色（建议HDR拉满）
        _Density ("Density", Range(1, 50)) = 10    // 闪光密度
        _Speed ("Speed", Range(0, 10)) = 3         // 闪烁速度
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
            fixed4 _SparkleColor;
            float _Density;
            float _Speed;

            // 一个简单的伪随机函数
            float random (float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
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
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                if (c.a < 0.1) discard;

                // --- 闪烁逻辑 ---
                
                // 1. 把 UV 坐标分块
                // floor(uv * N) 把图片分成 N x N 个格子
                float2 gridID = floor(i.uv * _Density);
                
                // 2. 为每个格子生成一个随机值 (0~1)
                float rnd = random(gridID);
                
                // 3. 让随机值随时间变化
                // sin(Time + RandomOffset) 让每个格子的闪烁相位不同
                float twinkle = sin(_Time.y * _Speed + rnd * 6.28);
                
                // 4. 锐化波形
                // 我们只想要偶尔闪一下，大部分时间不闪
                // pow(..., 20) 会把波形压得很扁，只有波峰是尖的
                twinkle = pow(0.5 + 0.5 * twinkle, 20.0);

                // 5. 再次引入随机性：有的格子闪，有的格子永远不闪
                if (rnd < 0.5) twinkle = 0;

                // 6. 叠加颜色
                // 只有原图亮的地方才闪 (c.rgb * 亮度)，暗部不闪
                float brightness = dot(c.rgb, float3(0.3, 0.59, 0.11));
                c.rgb += _SparkleColor.rgb * twinkle * brightness;

                return c;
            }
            ENDCG
        }
    }
}