



using System.Collections.Generic;

namespace GoveKits.Aether
{
    // ==========================================================
    // 6. AetherUniverse: 宇宙 (位面管理器)
    // ==========================================================
    /// <summary>
    /// 管理所有位面，提供默认主位面。
    /// </summary>
    public static class AetherUniverse
    {
        public static AetherPlane MainPlane { get; } = new AetherPlane("Main");
        private static readonly Dictionary<string, AetherPlane> _planes = new Dictionary<string, AetherPlane>
        {
            { "Main", MainPlane }
        };


        /// <summary>
        /// 创建新位面
        /// </summary>
        /// <param name="name"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static bool TryGetPlane(string name, out AetherPlane plane)
            => _planes.TryGetValue(name, out plane);



        /// <summary>
        /// 创建新位面
        /// </summary>
        public static AetherPlane CreatePlane(string name)
        {
            if (_planes.ContainsKey(name))
                throw new System.Exception($"AetherPlane with name '{name}' already exists.");

            var plane = new AetherPlane(name);
            _planes[name] = plane;
            return plane;
        }


        /// <summary>
        /// 删除位面
        /// </summary>
        public static bool RemovePlane(string name)
        {
            if (name == "Main") return false; // 主位面不可删除
            return _planes.Remove(name);
        }
    }
}