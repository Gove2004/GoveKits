

namespace GoveKits.Runtime.Unit
{
    public class CDMark : UnitMark
    {
        public override UnitTag Name { get; protected set; }

        /// <summary>
        /// 创建一个 CD 标记。
        /// </summary>
        /// <param name="Onwer"></param>
        /// <param name="name"></param>
        /// <param name="duration"></param>
        public CDMark(IUnit Onwer, string name, float duration) : base(Onwer, 1, duration)
        {
            Name = name;
        }
    }
}