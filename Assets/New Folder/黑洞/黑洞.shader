Shader "Custom/VortexDistortion_BlackHole"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _Twist ("Twist Strength", Range(-20, 20)) = 10 // 扭曲力度
        _Radius ("Radius", Range(0, 1)) = 0.6          // 扭曲范围
        _BlackHoleSize ("BlackHole Size", Range(0, 0.5)) = 0.15 // 核心黑洞大小
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
            float _Twist;
            float _Radius;
            float _BlackHoleSize;

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
                float2 uv = i.uv - 0.5;
                float dist = length(uv);
                float angle = atan2(uv.y, uv.x);

                // --- 1. 计算扭曲 ---
                // 使用 smoothstep 让边缘过渡更自然
                // 离中心越近，twistFactor 越大
                float twistFactor = 1.0 - smoothstep(0, _Radius, dist);
                
                // 加上时间让它转起来
                angle += (_Twist * twistFactor) + (_Time.y * 2.0 * twistFactor);

                // --- 2. 还原坐标 ---
                float2 twistedUV;
                sincos(angle, twistedUV.y, twistedUV.x);
                twistedUV *= dist;
                twistedUV += 0.5;

                // --- 3. 采样颜色 ---
                // [关键修复] 如果转出去了，强制透明 (防止Repeat模式产生的同心圆)
                if (twistedUV.x < 0 || twistedUV.x > 1 || twistedUV.y < 0 || twistedUV.y > 1)
                    return fixed4(0,0,0,0);

                fixed4 col = tex2D(_MainTex, twistedUV) * i.color;

                // --- 4. [新增] 绘制黑洞中心 ---
                // 计算黑洞遮罩：中心是0(黑)，边缘是1(原色)
                float hole = smoothstep(_BlackHoleSize, _BlackHoleSize + 0.1, dist);
                
                // 把中心颜色乘没了 -> 变黑
                col.rgb *= hole;

                return col;
            }
            ENDCG
        }
    }
}