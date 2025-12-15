Shader "Custom/Pixelation"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // 分辨率：值越小，像素颗粒越大；值越大，越清晰
        _PixelDensity ("Pixel Density", Range(1, 256)) = 64 
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
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float _PixelDensity;

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
                float2 uv = i.uv;

                // --- 核心算法：UV 取整 ---
                // 1. 把 UV 放大 N 倍 (比如 64 倍)
                uv *= _PixelDensity;
                
                // 2. 向下取整 (把小数扔掉，变成整数格子的坐标)
                uv = floor(uv);
                
                // 3. 再除以 N 倍缩回去
                // 结果：UV 不再是平滑的，而是阶梯状的
                uv /= _PixelDensity;

                fixed4 c = tex2D(_MainTex, uv) * i.color;
                
                return c;
            }
            ENDCG
        }
    }
}