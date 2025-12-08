using System.Collections.Generic;

namespace GoveKits.Events
{
    /// <summary>
    /// 事件频道。管理特定类型事件的所有监听器列表。
    /// </summary>
    public class EventChannel
    {
        private readonly List<EventListener> _listeners = new List<EventListener>();
        private bool _isDirty = false; // 标记列表是否需要重新排序

        public void Add(EventListener listener)
        {
            _listeners.Add(listener);
            _isDirty = true; // 标记为脏，下次发布时排序
        }

        public void Remove(EventListener listener)
        {
            _listeners.Remove(listener);
        }

        /// <summary>
        /// 向所有监听器广播事件。
        /// </summary>
        public void Publish(EventInfo evt)
        {
            if (_listeners.Count == 0) return;

            // 仅在列表变动后进行一次排序，减少性能开销
            if (_isDirty)
            {
                _listeners.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                _isDirty = false;
            }

            // 使用 for 循环避免 foreach 产生的 Enumerator GC
            // 注意：如果在 OnReceive 中移除了监听器，会导致索引偏移。
            // 考虑到高性能需求，暂不使用 ToArray() 副本。建议不要在事件回调中直接移除同类事件的监听器。
            // 倒序遍历是删除安全的，或者使用标记法
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                // 再次检查越界，虽然倒序通常安全
                if (i >= _listeners.Count) continue;
                
                _listeners[i].OnReceive(evt);
                if (evt.IsStopped) break;
            }
        }
    }
}