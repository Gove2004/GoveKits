Shader "Custom/WetReflection"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,0.5) // 倒影通常半透明
        
        [Header(Water Settings)]
        _WaveSpeed ("Wave Speed", Range(0, 10)) = 2
        _WaveAmp ("Wave Amplitude", Range(0, 0.05)) = 0.01 // 波浪幅度
        _Blur ("Blur Level", Range(0, 5)) = 0 // 简单的模糊模拟
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
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float _WaveSpeed;
            float _WaveAmp;
            float _Blur;

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
                float2 uv = i.uv;

                // 1. 水波扭曲
                // 利用屏幕坐标的 Y 值或者 UV 的 Y 值来做波形
                // 这样波浪是水平横向的
                float offset = sin(uv.y * 50 + _Time.y * _WaveSpeed) * _WaveAmp;
                uv.x += offset;

                // 2. 简单的模糊 (采样左右两边取平均)
                // 真正的模糊很费性能，这里用“偏移重叠”模拟一下
                fixed4 col = tex2D(_MainTex, uv);
                fixed4 colL = tex2D(_MainTex, uv + float2(_Blur * 0.005, 0));
                fixed4 colR = tex2D(_MainTex, uv - float2(_Blur * 0.005, 0));
                
                col = (col + colL + colR) / 3.0;

                // 3. 渐变消失 (Fade Out)
                // 倒影离脚越远(uv.y越小或越大)应该越淡
                // 假设 Sprite 是头朝下的，uv.y=1 是脚，uv.y=0 是头
                // 我们让头部(0)透明，脚部(1)不透明
                float fade = smoothstep(0, 1, uv.y); 
                
                // 应用颜色和透明度
                col *= i.color;
                col.a *= fade; // 远处消失

                return col;
            }
            ENDCG
        }
    }
}