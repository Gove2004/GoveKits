Shader "Custom/AnimeZoomBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0) // 聚焦中心
        _Strength ("Blur Strength", Range(0, 0.5)) = 0.1 // 模糊强度
        _Samples ("Quality", Int) = 10 // 采样次数(越高越细腻但越卡)
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
            float _Strength;
            int _Samples;

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
                // 1. 计算当前点到中心的向量
                float2 dir = _Center.xy - i.uv;
                
                // 2. 循环采样累加
                float4 sum = float4(0,0,0,0);
                
                for(int k = 0; k < _Samples; k++)
                {
                    // 每次采样都向中心靠近一点点
                    float scale = 1.0 - _Strength * (float(k) / float(_Samples));
                    
                    // 计算偏移后的 UV
                    // 公式：Center + (Current - Center) * Scale
                    float2 uv = _Center.xy + (i.uv - _Center.xy) * scale;
                    
                    sum += tex2D(_MainTex, uv);
                }

                // 3. 取平均值
                sum /= float(_Samples);

                return sum * i.color;
            }
            ENDCG
        }
    }
}