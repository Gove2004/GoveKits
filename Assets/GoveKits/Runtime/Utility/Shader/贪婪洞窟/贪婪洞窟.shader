Shader "Custom/GreedyCaveStyle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Paper Feel)]
        _NoiseTex ("Paper Noise (Gray)", 2D) = "white" {} // 核心：拖一张纸张纹理或云彩噪点图
        _PaperColor ("Paper Tint", Color) = (0.9, 0.85, 0.7, 1) // 羊皮纸的底色
        _NoiseStrength ("Paper Grain", Range(0, 1)) = 0.5 // 纸张颗粒强度
        
        [Header(Sketchy Outline)]
        _OutlineColor ("Outline Color", Color) = (0.1, 0.05, 0, 1) // 深褐色描边
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
        _Distort ("Edge Roughness", Range(0, 0.05)) = 0.01 // 边缘扭曲程度(手绘感来源)
        _Speed ("Wiggle Speed", Float) = 5.0 // 线条抖动速度(0就是静止手绘)
        
        [Header(Color Grading)]
        _Desaturation ("Desaturation", Range(0, 1)) = 0.5 // 去色程度
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
                float2 noiseUV  : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            sampler2D _NoiseTex; float4 _NoiseTex_ST;
            
            fixed4 _PaperColor;
            float _NoiseStrength;
            
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _Distort;
            float _Speed;
            float _Desaturation;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                o.noiseUV = TRANSFORM_TEX(v.texcoord, _NoiseTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. 生成抖动 UV (The Wiggle)
                // 让噪声图随时间快速切换位置，或者只是滚动
                float2 noiseOffset = float2(
                    sin(_Time.y * _Speed), 
                    cos(_Time.y * _Speed * 0.7)
                );
                
                // 采样噪声
                // 细节：用 noiseUV 采样，加上 offset
                fixed4 noise = tex2D(_NoiseTex, i.noiseUV + noiseOffset * 0.1);
                
                // 核心：用噪声值去偏移主 UV
                // (noise.r - 0.5) 把范围变到 -0.5 ~ 0.5
                float2 distortedUV = i.uv + (noise.xy - 0.5) * _Distort;

                // 2. 采样主纹理 (用扭曲后的 UV)
                fixed4 col = tex2D(_MainTex, distortedUV);
                
                // 3. 计算描边 (用 Alpha 判断)
                // 如果当前点是透明的，但它的 alpha 在“扭曲后”仍然接近实体，说明是边缘？
                // 这里我们用一种更简单的方法：利用 Alpha 的梯度
                // 因为 UV 被扭曲了，边缘已经变得坑坑洼洼了
                // 我们只需要检测 alpha 是否在 0.1 ~ 0.8 之间作为边缘
                
                float alpha = col.a;
                // 只要 alpha 不透明也不完全透明，就是边缘
                // 使用 smoothstep 让描边有点晕染感（墨水感）
                float isOutline = smoothstep(0.1, 0.4, alpha) * (1.0 - smoothstep(0.8, 1.0, alpha));
                
                // 增强描边：如果 UV 扭曲导致采到了外面，我们要把 alpha 修正一下
                col.rgb = lerp(col.rgb, _OutlineColor.rgb, isOutline * _OutlineColor.a * 2.0);
                
                // 4. 纸张质感叠加 (Paper Overlay)
                // 再次采样静态的噪声(不动的)，作为纸纹
                fixed paperGrain = tex2D(_NoiseTex, i.uv * 3.0).r;
                // 混合模式：Multiply (正片叠底)
                col.rgb *= lerp(fixed3(1,1,1), _PaperColor.rgb * paperGrain, _NoiseStrength);

                // 5. 去色 (Desaturation)
                float lum = dot(col.rgb, float3(0.3, 0.59, 0.11));
                fixed3 gray = fixed3(lum, lum, lum);
                col.rgb = lerp(col.rgb, gray, _Desaturation);

                // 6. 最终修正
                // 保证完全透明的地方不显示
                col.a *= i.color.a; 
                // 防止描边在完全透明区域乱画
                if (col.a < 0.1) discard;

                return col;
            }
            ENDCG
        }
    }
}