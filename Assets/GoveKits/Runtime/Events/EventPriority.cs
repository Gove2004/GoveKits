namespace GoveKits.Events
{
    /// <summary>
    /// 事件监听优先级定义。
    /// <para>数值越小，执行越早 (High Priority -> Low Priority)。</para>
    /// </summary>
    public static class EventPriority
    {
        public const int Instant = -10000;    // 最先执行：系统级拦截、作弊码
        public const int Highest = -1000;     // 极高：无敌判定、伤害完全抵消
        public const int High    = -500;      // 高：护盾计算、伤害减免
        public const int AboveNormal = -100;  // 较高
        public const int Normal  = 0;         // 标准：核心逻辑处理（扣血、播放特效）
        public const int BelowNormal = 100;   // 较低
        public const int Low     = 500;       // 低：统计数据、成就触发
        public const int Lowest  = 1000;      // 极低
        public const int Monitor = 10000;     // 最后执行：UI显示、日志打印（建议只读）
    }
}