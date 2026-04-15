using UnityEngine;

namespace GoveKits.Runtime.Util
{
    // 贝塞尔曲线相关算法。
    public static class Bezier
    {
        /// <summary>
        /// 计算 n 次贝塞尔曲线上的点。
        /// </summary>
        /// <param name="t">参数，范围 [0, 1]。</param
        /// <param name="controlPoints">控制点数组，长度决定了贝塞尔曲线的阶数。</param>
        public static Vector3 Calculate(float t, params Vector3[] controlPoints)
        {
            int n = controlPoints.Length - 1;
            Vector3 point = Vector3.zero;

            for (int i = 0; i <= n; i++)
            {
                float binomial = BinomialCoefficient(n, i);
                float term = binomial * Mathf.Pow(1 - t, n - i) * Mathf.Pow(t, i);
                point += term * controlPoints[i];
            }

            return point;
        }

        public static Vector2 Calculate(float t, params Vector2[] controlPoints)
        {
            int n = controlPoints.Length - 1;
            Vector2 point = Vector2.zero;

            for (int i = 0; i <= n; i++)
            {
                float binomial = BinomialCoefficient(n, i);
                float term = binomial * Mathf.Pow(1 - t, n - i) * Mathf.Pow(t, i);
                point += term * controlPoints[i];
            }

            return point;
        }

        /// <summary>
        /// 计算二项式系数 C(n, k) = n! / (k! * (n - k)!)，用于贝塞尔曲线的计算。
        /// </summary>
        /// <param name="n">总数量</param>
        /// <param name="k">选取数量</param>
        /// <returns>二项式系数</returns>
        private static float BinomialCoefficient(int n, int k)
        {
            if (k < 0 || k > n) return 0;
            if (k == 0 || k == n) return 1;

            float result = 1;
            for (int i = 1; i <= k; i++)
            {
                result *= (n - (k - i)) / (float)i;
            }
            return result;
        }
    }
}