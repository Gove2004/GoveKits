Shader "Custom/GamerRGB"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _Speed ("Rainbow Speed", Range(0, 10)) = 2
        _Saturation ("Saturation", Range(0, 1)) = 1 // 饱和度
        _Brightness ("Brightness", Range(0, 1)) = 1 // 亮度
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
            float _Speed;
            float _Saturation;
            float _Brightness;

            // --- 核心算法：HSV 转 RGB ---
            // 这是一个通用的数学公式，直接抄就行
            float3 HUEtoRGB(float H)
            {
                float R = abs(H * 6 - 3) - 1;
                float G = 2 - abs(H * 6 - 2);
                float B = 2 - abs(H * 6 - 4);
                return saturate(float3(R,G,B));
            }

            float3 HSVtoRGB(float3 HSV)
            {
                float3 RGB = HUEtoRGB(HSV.x);
                return ((RGB - 1) * HSV.y + 1) * HSV.z;
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
                fixed4 col = tex2D(_MainTex, i.uv);
                if (col.a < 0.1) discard;

                // 1. 计算当前的色相 (Hue)
                // _Time.y * Speed 随时间变化
                // + i.uv.x + i.uv.y 让颜色在空间上也有渐变（彩虹波浪）
                float hue = frac(_Time.y * _Speed * 0.1 + i.uv.x * 0.5 + i.uv.y * 0.5);

                // 2. 构建 HSV 向量
                float3 hsv = float3(hue, _Saturation, _Brightness);

                // 3. 转回 RGB
                float3 rainbowColor = HSVtoRGB(hsv);

                // 4. 混合
                // 这里我们直接用彩虹色替换原色
                // 但保留原图的 Alpha 和 亮度信息(用原图的灰度乘一下)
                float luminance = dot(col.rgb, float3(0.3, 0.59, 0.11));
                
                // 稍微保留一点原图的明暗关系，不然看起来像剪影
                col.rgb = rainbowColor * (luminance + 0.5); 

                return col;
            }
            ENDCG
        }
    }
}