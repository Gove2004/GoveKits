Shader "Custom/WindyFoliage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _WindSpeed ("Wind Speed", Range(0, 5)) = 2
        _WindStrength ("Wind Strength", Range(0, 0.5)) = 0.1
        _SwayFrequency ("Sway Frequency", Range(0, 10)) = 3 // 摆动频率
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float _WindSpeed;
            float _WindStrength;
            float _SwayFrequency;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                float4 worldPos = mul(unity_ObjectToWorld, IN.vertex);
                
                // --- 核心逻辑 ---
                
                // 1. 根部固定机制
                // 假设 Sprite 的 Pivot（轴心）在底部 (y=0 或 y更小)
                // 我们通过 UV 的 y 值来判断：y越小(底部)受影响越小，y越大(顶部)晃得越厉害
                float mask = IN.texcoord.y; 
                // 如果你的图 Pivot 在中心，可能需要改成 clamp(IN.vertex.y + 0.5, 0, 1)

                // 2. 计算风的波形
                // _Time.y * Speed : 时间驱动
                // + worldPos.x : 不同位置的草，相位不同（错开摇摆）
                float wind = sin(_Time.y * _WindSpeed + worldPos.x * 0.5);
                
                // 3. 叠加一点高频抖动（模拟树叶颤抖）
                float jitter = sin(_Time.y * _WindSpeed * 3 + worldPos.y) * 0.2;

                // 4. 应用偏移
                // 偏移量 = (大风 + 小抖动) * 强度 * 根部遮罩
                float xOffset = (wind + jitter) * _WindStrength * mask;

                float4 localPos = IN.vertex;
                localPos.x += xOffset;

                OUT.vertex = UnityObjectToClipPos(localPos);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                if(c.a < 0.1) discard;
                return c;
            }
            ENDCG
        }
    }
}