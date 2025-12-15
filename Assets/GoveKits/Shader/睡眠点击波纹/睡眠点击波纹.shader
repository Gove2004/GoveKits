Shader "Custom/WaterDropRipple"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // 这些参数由 C# 脚本控制
        _Center ("Center (UV)", Vector) = (0.5, 0.5, 0, 0) // 波心位置
        _StartTime ("Start Time", Float) = 0               // 点击的时间点
        
        [Header(Ripple Settings)]
        _Speed ("Wave Speed", Float) = 2.0                 // 扩散速度
        _Frequency ("Frequency", Float) = 15.0             // 波纹密度(几圈)
        _Amplitude ("Amplitude", Range(0, 0.1)) = 0.03     // 波纹起伏高度(扭曲力度)
        _MaxDist ("Max Distance", Range(0, 1)) = 0.5       // 波纹最大扩散范围
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
            float4 _Center;
            float _StartTime;
            float _Speed;
            float _Frequency;
            float _Amplitude;
            float _MaxDist;

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
                // 1. 计算时间流逝 (当前时间 - 点击时间)
                float t = _Time.y - _StartTime;
                
                // 如果时间是负数或者太久了，就不计算 (省性能)
                // if (t < 0) return tex2D(_MainTex, i.uv) * i.color;

                // 2. 计算到波心的距离
                // 修正长宽比: 如果图片不是正方形，波纹会变扁。
                // 这里为了简单，假设是正方形。如果不是，需要传入 AspectRatio 修正 uv.x
                float dist = distance(i.uv, _Center.xy);

                // 3. 计算波形
                // sin(距离 * 密度 - 时间 * 速度)
                float wave = sin(dist * _Frequency - t * _Speed);

                // 4. 计算衰减 (Mask)
                
                // 距离衰减：离中心越远，波越弱
                float distFade = 1.0 - smoothstep(0, _MaxDist, dist);
                
                // 时间衰减：时间过得越久，波越弱
                // 假设波纹持续 1.5 秒
                float timeFade = 1.0 - smoothstep(0, 1.5, t);
                
                // 边缘切断：波纹还没传到的地方不动
                // currentRadius = t * speed / frequency ... 粗略模拟一下
                // 只要 t > 0，我们就让全图都动，靠 distanceFade 来限制范围
                
                float totalFade = distFade * timeFade;
                
                // 如果衰减完了，就没波纹了
                if (totalFade <= 0) 
                {
                    return tex2D(_MainTex, i.uv) * i.color;
                }

                // 5. 计算最终扭曲 UV
                // 方向：从中心向外推 (i.uv - center)
                float2 offset = normalize(i.uv - _Center.xy) * wave * _Amplitude * totalFade;
                
                // 6. 采样
                fixed4 col = tex2D(_MainTex, i.uv + offset);
                
                // (可选) 加一点高光，模拟水面反光
                // 波峰处变亮
                col.rgb += wave * totalFade * 0.1;

                return col * i.color;
            }
            ENDCG
        }
    }
}