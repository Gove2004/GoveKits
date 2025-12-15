Shader "Custom/SSR_Shiny"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Shiny Settings)]
        [HDR] _ShinyColor ("Shiny Color", Color) = (1, 1, 0.5, 1) // 金色高光
        _Speed ("Sweep Speed", Range(0, 5)) = 1     // 扫光速度
        _Width ("Sweep Width", Range(0, 1)) = 0.4   // 光条宽度
        _Angle ("Angle", Range(-1, 1)) = 0.2        // 倾斜角度
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
                float2 worldPos : TEXCOORD1; // 用世界坐标来做光，防止图片旋转时光跟着转
            };

            fixed4 _Color;
            sampler2D _MainTex;
            fixed4 _ShinyColor;
            float _Speed;
            float _Width;
            float _Angle;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                // 获取像素在世界空间的位置
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                if (c.a < 0.1) discard;

                // --- 流光计算逻辑 ---
                
                // 1. 制造移动：用 时间 减去 位置
                // i.worldPos.x + i.worldPos.y * _Angle ：这决定了光的倾斜方向
                // _Time.y * _Speed ：这决定了光的移动
                float posValue = (i.worldPos.x + i.worldPos.y * _Angle) - _Time.y * _Speed;

                // 2. 制造条纹：用 sin 函数把数值变成波形
                // 0.5 是为了把正弦波的中心移到 0~1 之间
                // pow 是为了让光条变得更细、边缘更硬（指数级变陡）
                float lightBar = pow(sin(posValue * 3.0) * 0.5 + 0.5, 20.0);

                // 3. 叠加颜色
                // 只有在 lightBar > 0 的地方才加光
                // _ShinyColor * lightBar * _Width
                c.rgb += _ShinyColor.rgb * lightBar * _Width;

                return c;
            }
            ENDCG
        }
    }
}