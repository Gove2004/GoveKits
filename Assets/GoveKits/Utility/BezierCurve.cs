using System;
using System.Collections.Generic;
using UnityEngine;


namespace GoveKits.Utility
{
    public class BezierCurve
    {
        /// <summary>
        /// 计算贝塞尔曲线上的点
        /// </summary>
        public static Vector3 GetPoint(Vector3[] points, float t)
        {
            if (points == null || points.Length == 0) return Vector3.zero;

            Vector3[] temp = points; // 直接引用（但确保不修改原数组）
            int n = points.Length;

            for (int k = 1; k < n; k++)
            {
                for (int i = 0; i < n - k; i++)
                {
                    temp[i] = Vector3.Lerp(temp[i], temp[i + 1], t);
                }
            }
            return temp[0];
        }
    }
}