using System;
using System.Collections.Generic;

namespace GoveKits.Units
{
    public class AttributeContainer : DictionaryContainer<Attribute>
    {
        /// <summary>
        /// 添加基础属性
        /// </summary>
        public void Add(string key, float initialValue)
        {
            Add(key, new Attribute(key, initialValue));
        }

        /// <summary>
        /// 直接添加属性，会自动使用 As(key) 转换
        /// </summary>
        public override void Add(string key, Attribute attribute)
        {
            _items[key] = attribute.As(key);
        }

        /// <summary>
        /// 添加属性, 自动使用属性名作为键
        /// </summary>
        /// <param name="attribute"></param>
        public void Add(Attribute attribute)
        {
            Add(attribute.Name, attribute);
        }


        public bool TryGetValue(string key, out float value)
        {
            if (_items.TryGetValue(key, out var attribute))
            {
                value = attribute.Value;
                return true;
            }
            value = 0f;
            return false;
        }

        public void SetValue(string key, float value)
        {
            Attribute attribute = _items[key];
            attribute.Value = value;
        }

        public override void Clear()
        {
            foreach (var kvp in _items)
            {
                kvp.Value.Clear();
            }
            base.Clear();
        }


        // 添加监听器，返回取消监听的操作
        public Action AddListener(string key, Action<float, float> listener)
        {
            return _items[key].Subscribe(listener);
        }

        // 移除监听器
        public void RemoveListener(string key, Action<float, float> listener)
        {
            _items[key].Unsubscribe(listener);
        }
    }



    public static class AttributeContainerExtensions
    {
        // 一键添加线性属性，key = base * factor + bias
        public static void AppendLinear(this AttributeContainer container, string key, float baseValue, float factorValue = 1f, float biasValue = 0f)
        {
            string baseKey = key + "_Base";
            string factorKey = key + "_Factor";
            string biasKey = key + "_Bias";
            Attribute baseAttr = new Attribute(baseKey, baseValue);
            Attribute factorAttr = new Attribute(factorKey, factorValue);
            Attribute biasAttr = new Attribute(biasKey, biasValue);
            var computedAttr = ((baseAttr * factorAttr) + biasAttr).As(key);
            container.Add(baseKey, baseAttr);
            container.Add(factorKey, factorAttr);
            container.Add(biasKey, biasAttr);
            container.Add(key, computedAttr);
        }


        // 一键添加多个线性属性， keys = baseKeys * factor + bias
        public static void AppendLinearBatch(this AttributeContainer container, IEnumerable<string> keys, float factorValue = 1f, float biasValue = 0f)
        {
            foreach (var key in keys)
            {
                container.AppendLinear(key, 0f, factorValue, biasValue);
            }
        }


        // 多级线性， key = (base) * [(1 + multi_i)] + [add_i]
        public static void AppendMultiLinear(this AttributeContainer container, string key, float baseValue, IEnumerable<(float add, float multi)> modifiers)
        {
            string baseKey = key + "_Base";
            Attribute baseAttr = new Attribute(baseKey, baseValue);
            Attribute addSumAttr = new Attribute($"{key}_Add_Final", 0f);
            Attribute multiSumAttr = new Attribute($"{key}_Multi_Final", 1f);
            Attribute computedAttr = baseAttr;
            int index = 0;
            foreach (var (add, multi) in modifiers)
            {
                string addKey = $"{key}_Add_{index}";
                string multiKey = $"{key}_Multi_{index}";
                Attribute addAttr = new Attribute(addKey, add);
                Attribute multiAttr = new Attribute(multiKey, 1f + multi);
                addSumAttr = addSumAttr + addAttr;
                multiSumAttr = multiSumAttr * multiAttr;
                container.Add(addKey, addAttr);
                container.Add(multiKey, multiAttr);
                index++;
            }
            computedAttr = ((computedAttr * multiSumAttr) + addSumAttr).As(key);

            container.Add(baseKey, baseAttr);
            container.Add(key, computedAttr);
        }



        // 快照
        public static Dictionary<string, float> CreateSnapshot(this AttributeContainer container)
        {
            var snapshot = new Dictionary<string, float>();
            foreach (var key in container.Keys)
            {
                snapshot[key] = container.TryGetValue(key, out var val) ? val : 0f;
            }
            return snapshot;
        }
        public static void ApplySnapshot(this AttributeContainer container, Dictionary<string, float> snapshot)
        {
            foreach (var kvp in snapshot)
            {
                if (container.Has(kvp.Key))
                {
                    container.SetValue(kvp.Key, kvp.Value);
                }
            }
        }
    }
}