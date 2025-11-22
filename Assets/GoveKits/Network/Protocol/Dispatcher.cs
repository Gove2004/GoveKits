using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    // 1. 定义非泛型接口，供 Dispatcher 统一存储
    public interface IMessageHandler
    {
        UniTask Handle(Message message);
    }

    // 2. 泛型基类，自动处理类型转换，开发者只需继承这个
    public class MessageHandler<TMsg> : IMessageHandler where TMsg : Message
    {
        private readonly Action<TMsg> _handlerAction;
        public MessageHandler(Action<TMsg> handlerAction)
        {
            _handlerAction = handlerAction;
        }
        public async UniTask Handle(Message message)
        {
            if (message is TMsg tMsg)
            {
                await Run(tMsg);
            }
            else
            {
                Debug.LogError($"[MessageHandler] Type mismatch. Handler expects {typeof(TMsg).Name}, got {message.GetType().Name}");
            }
        }

        protected UniTask Run(TMsg msg)
        {
            _handlerAction?.Invoke(msg);
            return UniTask.CompletedTask;
        }
    }


    // 标记方法为消息处理器, 自动根据参数类型注册ID
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageHandlerAttribute : Attribute
    {
        public int Id { get; }
        public MessageHandlerAttribute(int id) => Id = id;
    }


    
    // 3. 消息分发器
    public class MessageDispatcher
    {
        // 消息ID -> Handler列表 (用于分发)
        private readonly Dictionary<int, List<IMessageHandler>> _msgMap = new();

        // 目标对象 -> (消息ID, Handler实例)列表 (用于反注册)
        private readonly Dictionary<object, List<(int id, IMessageHandler handler)>> _targetMap = new();

        // --- 核心分发逻辑 ---
        public async UniTask DispatchAsync(Message msg)
        {
            if (_msgMap.TryGetValue(msg.MsgID, out var list))
            {
                // 倒序遍历，防止处理过程中Unregister导致报错
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    try { await list[i].Handle(msg); }
                    catch (Exception ex) { Debug.LogError($"[Dispatcher] Error: {ex}"); }
                }
            }
        }

        // --- 智能绑定 ---
    public void Bind(object target)
    {
        if (_targetMap.ContainsKey(target)) return; // 防止重复绑定

        var bindings = new List<(int, IMessageHandler)>();
        
        // 扫描所有方法
        var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        foreach (var method in methods)
        {
            // 1. 找标记
            var attr = method.GetCustomAttribute<MessageHandlerAttribute>();
            if (attr == null) continue;

            // 2. 【安全检查】获取参数并校验
            var parameters = method.GetParameters();
            if (parameters.Length != 1) 
            {
                Debug.LogError($"[Bind Error] 方法 {target.GetType().Name}.{method.Name} 参数数量错误，必须只有一个参数。");
                continue;
            }

            Type msgType = parameters[0].ParameterType;
            if (!typeof(Message).IsAssignableFrom(msgType))
            {
                Debug.LogError($"[Bind Error] 方法 {target.GetType().Name}.{method.Name} 参数类型错误，必须继承自 Message。");
                continue;
            }

            // 3. 获取消息ID 
            // (如果你想手动在Attribute填ID，就用这行)
            int msgId = attr.Id;  
            
            // (如果你想自动推导，不写ID，改成下面这行，前提是Message类上有Attribute)
            // int msgId = msgType.GetCustomAttribute<NetMessageAttribute>()?.Id ?? 0; 

            // 4. 【关键修复】创建强类型委托
            try 
            {
                // 4.1 构造泛型 Handler 类型：MessageHandler<HeartbeatMessage>
                Type handlerType = typeof(MessageHandler<>).MakeGenericType(msgType);

                // 4.2 构造泛型 Action 类型：Action<HeartbeatMessage>
                Type actionType = typeof(Action<>).MakeGenericType(msgType);

                // 4.3 创建委托 (这一步是将 method 转换为 Action<T>)
                // Delegate.CreateDelegate 性能比 MethodInfo.Invoke 快得多
                Delegate actionDelegate = Delegate.CreateDelegate(actionType, target, method);

                // 4.4 创建 Handler 实例，传入委托
                // 假设 MessageHandler 构造函数是 public MessageHandler(Action<T> action)
                var handlerInstance = (IMessageHandler)Activator.CreateInstance(handlerType, actionDelegate);

                // 5. 存入字典
                Register(msgId, handlerInstance);
                bindings.Add((msgId, handlerInstance));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bind Error] 绑定 {method.Name} 失败: {ex.Message}");
            }
        }

        if (bindings.Count > 0)
        {
            _targetMap[target] = bindings;
        }
    }

        // --- 智能解绑 ---
        public void Unbind(object target)
        {
            if (_targetMap.TryGetValue(target, out var list))
            {
                foreach (var (id, handler) in list)
                {
                    Unregister(id, handler);
                }
                _targetMap.Remove(target);
            }
        }

        // --- 基础增删 ---
        private void Register(int id, IMessageHandler handler)
        {
            if (!_msgMap.ContainsKey(id)) _msgMap[id] = new List<IMessageHandler>();
            _msgMap[id].Add(handler);
        }

        private void Unregister(int id, IMessageHandler handler)
        {
            if (_msgMap.TryGetValue(id, out var list))
            {
                list.Remove(handler);
            }
        }
    }
}