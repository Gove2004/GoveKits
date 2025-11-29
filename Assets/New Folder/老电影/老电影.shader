Shader "Custom/OldFilmFilter"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _GrainAmount ("Grain Strength", Range(0, 1)) = 0.2 // 噪点强度
        _ScratchAmount ("Scratch Strength", Range(0, 1)) = 0.5 // 划痕强度
        _FlickerSpeed ("Flicker Speed", Float) = 10.0 // 闪烁速度
        _Vignette ("Vignette", Range(0, 1)) = 0.5 // 暗角
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
            float _GrainAmount;
            float _ScratchAmount;
            float _FlickerSpeed;
            float _Vignette;

            // 伪随机
            float rand(float2 co) {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
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
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                
                // 1. 转灰度 (Sepia 色调)
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                fixed3 sepia = fixed3(lum * 1.2, lum * 1.0, lum * 0.8); // 微微泛黄
                
                // 2. 生成动态噪点
                // 使用 uv + time 作为种子
                float grain = rand(i.uv + _Time.y * 10.0);
                sepia += (grain - 0.5) * _GrainAmount;

                // 3. 生成随机划痕
                // 划痕只在 X 轴随机，Y 轴是连贯的线
                float scratchX = i.uv.x + rand(float2(_Time.y * 10, 0)) * 0.01;
                // 用 sin 做几个随机的线条位置
                float scratch = sin(scratchX * 500.0 + _Time.y); 
                // 只保留极细的线
                scratch = 1.0 - step(0.99, scratch); // 0.99 阈值，只有极少数点是白
                
                // 随机出现划痕
                float showScratch = step(0.8, rand(float2(_Time.y, 1.0)));
                sepia += scratch * _ScratchAmount * showScratch;

                // 4. 整体亮度闪烁 (Flicker)
                float flicker = sin(_Time.y * _FlickerSpeed) * 0.05 + 0.95;
                sepia *= flicker;

                // 5. 暗角 (Vignette)
                float dist = distance(i.uv, float2(0.5, 0.5));
                sepia *= smoothstep(0.8, 0.8 - _Vignette, dist);

                return fixed4(sepia, col.a);
            }
            ENDCG
        }
    }
}