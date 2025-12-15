Shader "Custom/GlacialFreeze"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Ice Settings)]
        _FrostTex ("Frost Texture", 2D) = "white" {} // 冰霜纹理(噪声图)
        _IceColor ("Ice Color", Color) = (0.6, 0.9, 1, 1) // 冰的青色
        _FrostIntensity ("Frost Intensity", Range(0, 1)) = 0.8 // 结霜程度
        [HDR] _RimColor ("Rim Color", Color) = (1, 1, 1, 1) // 边缘反光(强白)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0 // 反光范围
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
                float2 frostUV  : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            sampler2D _FrostTex; float4 _FrostTex_ST;
            fixed4 _IceColor;
            float _FrostIntensity;
            fixed4 _RimColor;
            float _RimPower;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                o.frostUV = TRANSFORM_TEX(v.texcoord, _FrostTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                if (col.a < 0.1) discard;

                // 1. 采样冰霜纹理
                fixed4 frost = tex2D(_FrostTex, i.frostUV);
                
                // 2. 混合冰色
                // 用冰霜纹理来决定哪里更蓝，哪里更白
                fixed3 icyVisual = lerp(col.rgb, _IceColor.rgb, _FrostIntensity);
                
                // 叠加冰霜纹理的白色细节
                icyVisual += frost.rgb * 0.3 * _FrostIntensity;

                // 3. 计算边缘光 (2D 伪造版)
                // 在 2D 里，我们没有法线。
                // 但通常 alpha 通道的边缘就是物体的边缘。
                // 我们可以用原图 alpha 和 "稍微缩小一点的原图 alpha" 做差，得到边缘。
                // 这里用一个更简单的方法：利用纹理自身的灰度梯度模拟厚度
                float rim = 1.0 - col.a; // 这在半透明边缘有效
                
                // 或者简单粗暴：颜色越浅的地方越像反光？
                // 不，我们直接给整体加一层通透的高光
                float shine = frost.r * 0.5; 
                
                // 混合
                col.rgb = icyVisual + shine;

                // 4. 强行提亮边缘 (Rim)
                // 如果 Alpha 在 0.1 到 0.5 之间，说明是边缘
                if (col.a > 0.1 && col.a < 0.8)
                {
                    col.rgb += _RimColor.rgb;
                }

                return col;
            }
            ENDCG
        }
    }
}