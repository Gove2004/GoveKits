Shader "Custom/SketchOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        [Header(Settings)]
        _OutlineColor ("Line Color", Color) = (0,0,0,1)  // 线条颜色（默认黑色）
        _BackgroundColor ("Paper Color", Color) = (1,1,1,0) // 纸张颜色（默认透明，想变白纸就把A改成1）
        
        _Thickness ("Line Thickness", Range(0, 5)) = 1.5 // 线条粗细
        _Sensitivity ("Sensitivity", Range(0, 1)) = 0.5  // 灵敏度（越小细节越多）
        
        [Toggle] _ShowOriginal ("Show Original Color", Float) = 0 // 勾上显示原图，不勾显示素描
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
        }
        
        // 混合模式：根据你的需求调整。这里设为标准透明混合
        Blend SrcAlpha OneMinusSrcAlpha 
        Cull Off 
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // 纹理尺寸
            
            fixed4 _OutlineColor;
            fixed4 _BackgroundColor;
            float _Thickness;
            float _Sensitivity;
            float _ShowOriginal;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // --- Sobel 核心函数：计算一点周围的颜色差异 ---
            float GetEdgeWeight(float2 uv, float2 offset)
            {
                // 采样中心点周围的几个关键点
                fixed4 s1 = tex2D(_MainTex, uv + float2(-offset.x, -offset.y)); // 左下
                fixed4 s2 = tex2D(_MainTex, uv + float2(0, -offset.y));         // 下
                fixed4 s3 = tex2D(_MainTex, uv + float2(offset.x, -offset.y));  // 右下
                fixed4 s4 = tex2D(_MainTex, uv + float2(-offset.x, 0));         // 左
                // fixed4 s5 = 中心点 (不需要)
                fixed4 s6 = tex2D(_MainTex, uv + float2(offset.x, 0));          // 右
                fixed4 s7 = tex2D(_MainTex, uv + float2(-offset.x, offset.y));  // 左上
                fixed4 s8 = tex2D(_MainTex, uv + float2(0, offset.y));          // 上
                fixed4 s9 = tex2D(_MainTex, uv + float2(offset.x, offset.y));   // 右上

                // Sobel 算子公式 (水平和垂直方向的梯度)
                // 简单来说就是：左边减右边，上面减下面，算算差多少
                float4 Gx = -1*s1 -2*s4 -1*s7 + 1*s3 + 2*s6 + 1*s9;
                float4 Gy = -1*s1 -2*s2 -1*s3 + 1*s7 + 2*s8 + 1*s9;

                // 算出差异的大小 (RGB只要有一个通道变了就算变了)
                // dot(xx, 1) 是把 RGB 值加起来
                float edge = dot(abs(Gx) + abs(Gy), 1);
                
                return edge;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 计算线宽对应的 UV 偏移量
                float2 offset = _MainTex_TexelSize.xy * _Thickness;

                // 2. 运行 Sobel 算法，得到 edge 值 (0代表平坦，很大代表边缘)
                float edge = GetEdgeWeight(i.uv, offset);
                
                // 3. 采样原图颜色
                fixed4 originalCol = tex2D(_MainTex, i.uv);
                
                // 4. 判断是否是边缘
                // 如果 edge > 灵敏度，就是线条；否则就是背景
                // step 函数：如果 edge < _Sensitivity 返回 0，否则返回 1
                float isEdge = step(_Sensitivity, edge);

                // --- 最终输出逻辑 ---
                
                // 如果你要“画板风格” (不显示原图)
                if (_ShowOriginal < 0.5)
                {
                    // 如果原图这里是完全透明的，我们也保持透明
                    if (originalCol.a < 0.1) discard;

                    // 如果是边缘，显示线色；否则显示纸色
                    return lerp(_BackgroundColor, _OutlineColor, isEdge);
                }
                // 如果你要“保留原图风格” (只叠加描边)
                else
                {
                    // 在原图上面叠一层线条
                    return lerp(originalCol, _OutlineColor, isEdge * originalCol.a);
                }
            }
            ENDCG
        }
    }
}