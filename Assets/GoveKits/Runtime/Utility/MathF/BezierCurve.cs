using System;
using UnityEngine;

namespace GoveKits.MathF
{
    /// <summary>
    /// 贝塞尔曲线计算工具：支持任意阶数的贝塞尔曲线求值。
    /// 使用递推法计算，小规模控制点用栈缓冲避免 GC。
    /// </summary>
    public class BezierCurve
    {
        /// <summary>
        /// 计算贝塞尔曲线上的点。
        /// 使用递推法：每次迭代将控制点数减 1，直至得到曲线上的点。
        /// 对于小规模点集（≤16 个），使用栈分配以避免堆分配和 GC；
        /// 大规模则退回堆分配。
        /// </summary>
        /// <param name="points">贝塞尔曲线的控制点数组。</param>
        /// <param name="t">参数值，范围 [0,1]（会被钳制）。</param>
        /// <returns>参数 t 对应的曲线上的点。若点数为 0 返回 zero，为 1 返回该点本身。</returns>
        public static Vector3 GetPoint(Vector3[] points, float t)
        {
            if (points == null || points.Length == 0) return Vector3.zero;
            if (points.Length == 1) return points[0];

            int n = points.Length;
            float tt = Mathf.Clamp01(t);

            // 小 n 走栈上缓冲避免 GC，大 n 退回堆分配
            Span<Vector3> temp = n <= 16 ? stackalloc Vector3[n] : new Vector3[n];
            for (int i = 0; i < n; i++) temp[i] = points[i];

            for (int k = 1; k < n; k++)
            {
                for (int i = 0; i < n - k; i++)
                {
                    temp[i] = Vector3.LerpUnclamped(temp[i], temp[i + 1], tt);
                }
            }
            return temp[0];
        }
    }
}