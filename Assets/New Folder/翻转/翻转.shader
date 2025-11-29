Shader "Custom/Fake3DFlip"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _Speed ("Rotation Speed", Float) = 2.0
        _Width ("Width Scale", Range(0, 1)) = 1.0 // 调试用，一般由代码控制
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off // 关闭剔除，这样才能看到“背面”

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
            float _Speed;

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
                // 1. 计算旋转因子 (-1 到 1)
                float rotate = cos(_Time.y * _Speed);
                
                // 2. 修正 UV
                // 我们要让 UV.x 以 0.5 为中心缩放
                float2 uv = i.uv;
                
                // 把中心移到 0
                uv.x -= 0.5;
                
                // 核心：除以旋转因子，相当于拉伸/压缩 UV
                uv.x = uv.x / rotate;
                
                // 移回 0.5
                uv.x += 0.5;

                // 3. 裁剪
                // 如果 UV 超出了 0-1 的范围，说明是旋转时的“空白区”，不显示
                if (uv.x < 0 || uv.x > 1) discard;

                // 4. 采样
                fixed4 col = tex2D(_MainTex, uv);
                
                // 5. 背面变暗 (可选)
                // 如果 rotate 是负数，说明转到背面了
                if (rotate < 0)
                {
                    // 这里你可以换成另一张图(卡牌背面)
                    // 或者简单地把它变暗
                    col.rgb *= 0.5; 
                }

                return col * i.color;
            }
            ENDCG
        }
    }
}