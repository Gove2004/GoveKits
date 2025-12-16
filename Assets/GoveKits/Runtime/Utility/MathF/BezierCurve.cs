using System;
using UnityEngine;


namespace GoveKits.MathF
{
    public class BezierCurve
    {
        /// <summary>
        /// 计算贝塞尔曲线上的点
        /// </summary>
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