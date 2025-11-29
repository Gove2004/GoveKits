Shader "Custom/GodModeOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(GOD MODE SETTINGS)]
        _OutlineColor ("Outline Color", Color) = (1,1,1,1) // 纯白
        _CanvasScale ("Canvas Expand", Range(1.0, 3.0)) = 1.5 // 1.画布放大倍数（最重要！）
        _OutlineWidth ("Outline Width", Range(0, 0.5)) = 0.05 // 2.描边宽度
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True" 
        }

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
            
            fixed4 _OutlineColor;
            float _CanvasScale;
            float _OutlineWidth;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                
                // --- 黑科技步骤 1：顶点外扩 ---
                // 以中心点(0,0,0)为基准，把模型直接放大
                // 这样就有地方画描边了，不用求美术改图
                float4 expandedVertex = IN.vertex;
                expandedVertex.xy *= _CanvasScale; 
                
                OUT.vertex = UnityObjectToClipPos(expandedVertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // --- 黑科技步骤 2：UV 重映射 ---
                // 因为模型放大了，贴图也会被拉伸。
                // 我们需要把 UV 缩放回去，让角色保持原大小，居中显示
                float2 center = float2(0.5, 0.5);
                float2 realUV = (IN.texcoord - center) * _CanvasScale + center;

                // 检查：如果 UV 超出了 0-1 的范围，说明这里是“扩充出来的空地”
                // 先把这一块标记为透明，后面用来画描边
                fixed4 c = (realUV.x < 0 || realUV.x > 1 || realUV.y < 0 || realUV.y > 1) 
                            ? fixed4(0,0,0,0) 
                            : tex2D(_MainTex, realUV);
                            
                c.rgb *= c.a;

                // --- 步骤 3：实心描边计算 ---
                float maxAlpha = 0;
                
                // 暴力循环 8 个方向即可，因为 Canvas 够大，不需要螺旋采样也能很宽
                // 为了性能，我们只采 8 个点，但偏移量设大
                float2 directions[8] = {
                    float2(1,0), float2(-1,0), float2(0,1), float2(0,-1),
                    float2(0.7,0.7), float2(-0.7,0.7), float2(0.7,-0.7), float2(-0.7,-0.7)
                };

                // 根据 CanvasScale 修正描边宽度
                float realWidth = _OutlineWidth * _CanvasScale;

                for(int k=0; k<8; k++)
                {
                    // 去找原来的 UV 位置
                    float2 sampleUV = realUV + directions[k] * realWidth;
                    
                    // 如果采样点在合法的图片范围内
                    if(sampleUV.x >= 0 && sampleUV.x <= 1 && sampleUV.y >= 0 && sampleUV.y <= 1)
                    {
                        maxAlpha = max(maxAlpha, tex2D(_MainTex, sampleUV).a);
                    }
                }

                // --- 最终合成 ---
                // 如果当前位置是空的，但是周围有东西 -> 显示白色描边
                if (c.a < 0.1 && maxAlpha > 0.1)
                {
                    return _OutlineColor;
                }

                return c * IN.color;
            }
            ENDCG
        }
    }
}