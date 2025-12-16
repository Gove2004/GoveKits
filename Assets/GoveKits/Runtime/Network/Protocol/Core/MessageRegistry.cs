using System;
using System.Collections.Generic;
using System.Reflection;
using Google.Protobuf;
using UnityEngine;

namespace GoveKits.Network
{
    public static class MessageRegistry
    {
        private static readonly Dictionary<int, MessageParser> _parsers = new Dictionary<int, MessageParser>();
        private static readonly Dictionary<Type, int> _ids = new Dictionary<Type, int>();

        public static void Register(int id, Type type, MessageParser parser)
        {
            if (_parsers.ContainsKey(id)) return;
            _parsers[id] = parser;
            _ids[type] = id;
        }

        public static MessageParser GetParser(int id) => _parsers.TryGetValue(id, out var p) ? p : null;
        public static int GetId(Type type) => _ids.TryGetValue(type, out var id) ? id : -1;

        public static void ScanAndRegister<TEnum>() where TEnum : Enum
        {
            Type enumType = typeof(TEnum);
            string namespaceName = enumType.Namespace; 
            Assembly assembly = enumType.Assembly;

            string[] names = Enum.GetNames(enumType);
            Array values = Enum.GetValues(enumType);

            int count = 0;

            for (int i = 0; i < names.Length; i++)
            {
                string enumName = names[i];
                int msgId = (int)values.GetValue(i);
                if (msgId <= 0) continue;

                string className = enumName;
                if (className.EndsWith("Id")) className = className.Substring(0, className.Length - 2); 

                string fullClassName = string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}";
                Type msgType = assembly.GetType(fullClassName);

                if (msgType == null)
                {
                    LogManager.LogError("Registry", $"Class '{fullClassName}' not found for Enum '{enumName}'");
                    continue;
                }

                PropertyInfo parserProp = msgType.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
                if (parserProp == null) continue;

                var parser = parserProp.GetValue(null) as MessageParser;
                Register(msgId, msgType, parser);
                count++;
            }
            LogManager.Log("Registry", $"Registered {count} messages from {enumType.Name}");
        }
    }
}