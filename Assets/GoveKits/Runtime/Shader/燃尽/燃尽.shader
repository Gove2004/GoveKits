Shader "Custom/BurnDissolve_Pro"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Noise Settings)]
        _NoiseTex ("Noise Texture", 2D) = "white" {} // 必须拖黑白噪点图！
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 1
        
        [Header(Burn Settings)]
        _BurnAmount ("Dissolve Progress", Range(0, 1.1)) = 0 // 进度条
        
        [Header(Colors)]
        [HDR] _FireColor ("Fire Color", Color) = (4, 1.5, 0.5, 1) // HDR高亮色(R=4)
        _CharColor ("Char Color", Color) = (0, 0, 0, 1)       // 焦黑颜色
        
        [Header(Widths)]
        _FireWidth ("Fire Width", Range(0, 0.2)) = 0.05       // 火光宽度
        _CharWidth ("Char Width", Range(0, 0.2)) = 0.05       // 焦黑宽度
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True" 
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float2 noiseUV  : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float _NoiseScale;
            
            float _BurnAmount;
            fixed4 _FireColor;
            fixed4 _CharColor;
            float _FireWidth;
            float _CharWidth;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                
                // 允许调节噪声图的缩放，不用去改材质Tiling了
                OUT.noiseUV = TRANSFORM_TEX(IN.texcoord, _NoiseTex) * _NoiseScale;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                if (c.a < 0.1) discard;

                // 1. 采样噪声
                float noiseVal = tex2D(_NoiseTex, IN.noiseUV).r;

                // --- 修复核心：重新定义燃烧进度 ---
                // 我们算出总的边缘宽度
                float totalBorder = _FireWidth + _CharWidth;

                // 关键修复点：
                // 让阈值(Threshold)从一个“负数”开始。
                // 比如：进度0的时候，阈值是 -0.2。
                // 这样，哪怕噪声值是 0， 0 - (-0.2) = 0.2，也大于边缘宽度，就不会显示火光了。
                
                // 这里的 1.2 是为了保证进度条拉到 1 的时候能烧干净
                float threshold = _BurnAmount * (1.0 + totalBorder * 2) - totalBorder;

                // 计算当前像素和阈值的差距
                float diff = noiseVal - threshold;

                // --- 阶段 1：裁剪 (Clip) ---
                // 差距小于 0，说明这个像素的噪声值已经被阈值盖过了 -> 消失
                if (diff < 0) discard;

                // --- 阶段 2：计算燃烧层 (Fire) ---
                // 如果差距在 [0, FireWidth] 之间 -> 烈火
                // 用 1.0 - smoothstep 让越靠近消失点的地方越亮
                float fireStep = 1.0 - smoothstep(0, _FireWidth, diff);
                
                // --- 阶段 3：计算焦黑层 (Char) ---
                // 如果差距在 [FireWidth, TotalWidth] 之间 -> 焦黑
                float charStep = 1.0 - smoothstep(_FireWidth, totalBorder, diff);
                
                // --- 颜色混合逻辑 ---
                
                // 1. 先染焦黑 (只要 charStep > 0)
                c.rgb = lerp(c.rgb, _CharColor.rgb, charStep);

                // 2. 再染烈火 (只要 fireStep > 0)
                c.rgb = lerp(c.rgb, _FireColor.rgb, fireStep);

                // 3. 烈火区域强制不透明 (防止在半透明边缘看起来太淡)
                // 并且让火光稍微溢出一点，看起来更亮
                if (fireStep > 0.01) 
                {
                    c.a = max(c.a, 1); 
                    c.rgb += _FireColor.rgb * fireStep * 0.5; // 额外加亮
                }

                return c;
            }
            ENDCG
        }
    }
}