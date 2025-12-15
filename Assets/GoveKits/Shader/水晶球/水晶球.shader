Shader "Custom/Flashlight2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _Center ("Center (UV)", Vector) = (0.5, 0.5, 0, 0) // 光源中心(0-1)
        _Radius ("Radius", Range(0, 1)) = 0.3              // 光照半径
        _Smoothness ("Edge Softness", Range(0, 1)) = 0.2   // 边缘柔和度
        _Darkness ("DarknessColor", Color) = (0,0,0,0.8)   // 黑暗区域颜色
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
            float _Radius;
            float _Smoothness;
            fixed4 _Darkness;

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
                
                // 1. 计算距离
                // 为了修正图片长宽比不同导致的变形成椭圆，通常需要外部传入长宽比
                // 这里简单起见直接算 UV 距离
                float dist = distance(i.uv, _Center.xy);

                // 2. 计算光照遮罩 (1 = 亮，0 = 黑)
                // smoothstep(内圈, 外圈, 距离)
                // 距离小于内圈是0(我们反过来要1)，大于外圈是1(我们要0)
                float lightMask = 1.0 - smoothstep(_Radius, _Radius + _Smoothness, dist);

                // 3. 混合颜色
                // 如果 mask 是 1，显示原图 (col)
                // 如果 mask 是 0，显示黑暗色 (col * Darkness)
                // 这里的混合有点技巧：我们希望保留原图的纹理，只是变暗
                
                fixed3 finalRGB = lerp(col.rgb * _Darkness.rgb, col.rgb, lightMask);
                
                // Alpha 也要处理，如果 Darkness 是半透明黑，那就在这里混合
                return fixed4(finalRGB, col.a);
            }
            ENDCG
        }
    }
}