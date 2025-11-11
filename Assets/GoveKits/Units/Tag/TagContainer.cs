


using System;
using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Units
{
    // 标签容器，用于管理单位的标签
    public class GameplayTagContainer
    {
        private readonly HashSet<GameplayTag> _tags = new HashSet<GameplayTag>();
        public event Action<GameplayTag> OnTagAdded;    // 标签添加事件
        public event Action<GameplayTag> OnTagRemoved; // 标签移除事件

        // 添加标签
        public bool AddTag(GameplayTag tag)
        {
            if (tag == null || _tags.Contains(tag)) return false;

            _tags.Add(tag);
            OnTagAdded?.Invoke(tag);
            return true;
        }

        // 添加标签（通过字符串）
        public bool AddTag(string tagName) => AddTag(new GameplayTag(tagName));

        // 移除标签
        public bool RemoveTag(GameplayTag tag)
        {
            if (tag == null || !_tags.Contains(tag)) return false;

            _tags.Remove(tag);
            OnTagRemoved?.Invoke(tag);
            return true;
        }

        // 移除标签（通过字符串）
        public bool RemoveTag(string tagName) => RemoveTag(new GameplayTag(tagName));

        // 批量操作
        public void AddTags(params string[] tagNames)
        {
            foreach (var tagName in tagNames)
            {
                AddTag(tagName);
            }
        }

        public void RemoveTags(params string[] tagNames)
        {
            foreach (var tagName in tagNames)
            {
                RemoveTag(tagName);
            }
        }

        // 查询
        public bool HasTag(GameplayTag tag)
        {
            return tag != null && _tags.Contains(tag);
        }

        public bool HasTag(string tagName) => HasTag(new GameplayTag(tagName));

        public bool HasAny(params string[] tagNames) => tagNames.Any(HasTag);
        public bool HasAll(params string[] tagNames) => tagNames.All(HasTag);
        public bool HasNone(params string[] tagNames) => !tagNames.Any(HasTag);

        public bool Query(ITagQuery query)
        {
            return query.Matches(this);
        }

        // 获取所有标签
        public IReadOnlyCollection<GameplayTag> GetAllTags() => _tags;

        // 清空所有标签
        public void Clear()
        {
            var tagsToRemove = _tags.ToList();
            foreach (var tag in tagsToRemove)
            {
                RemoveTag(tag);
            }
        }

        // 获取标签数量
        public int Count => _tags.Count;
    }
}