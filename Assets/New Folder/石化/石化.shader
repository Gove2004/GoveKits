Shader "Custom/Petrification"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Stone Effect)]
        _StoneTex ("Stone Texture", 2D) = "gray" {} // 拖石头图
        _Progress ("Stone Progress", Range(0, 1)) = 0 // 0=正常，1=全石化
        _EdgeWidth ("Edge Width", Range(0, 0.1)) = 0.05
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
                float2 stoneUV  : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            sampler2D _StoneTex; float4 _StoneTex_ST;
            float _Progress;
            float _EdgeWidth;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                // 石头纹理通常需要平铺，所以单独算 UV
                o.stoneUV = TRANSFORM_TEX(v.texcoord, _StoneTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                if (col.a < 0.1) discard;

                // 1. 灰度计算 (Grayscale)
                // 心理学公式：人眼对绿色的亮度最敏感
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                
                // 2. 采样石头纹理
                fixed4 stone = tex2D(_StoneTex, i.stoneUV);
                
                // 3. 混合灰度和石头纹理 (让石头看起来像角色的形状)
                fixed3 finalStoneColor = gray * stone.rgb * 1.5; // *1.5是为了亮一点

                // 4. 计算进度 (从下往上)
                // 这里的 i.uv.y 就是从 0(脚) 到 1(头)
                // step(a, b): 如果 b >= a 返回 1，否则 0
                float isStone = step(i.uv.y, _Progress);
                
                // 5. 混合
                col.rgb = lerp(col.rgb, finalStoneColor, isStone);

                // 6. (可选) 加一条发光的边缘线
                float isEdge = step(i.uv.y, _Progress + _EdgeWidth) - isStone;
                if (isEdge > 0) col.rgb += fixed3(0.5, 1, 0.5); // 绿色的魔法边缘

                return col;
            }
            ENDCG
        }
    }
}