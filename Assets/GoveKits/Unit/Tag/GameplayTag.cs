


using System;
using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Units
{
    // 简化的扁平标签
    public class GameplayTag : IEquatable<GameplayTag>
    {
        public string Name { get; }

        public GameplayTag(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString() => Name;
        public override int GetHashCode() => Name.GetHashCode();
        public override bool Equals(object obj) => Equals(obj as GameplayTag);
        public bool Equals(GameplayTag other) => other != null && Name == other.Name;

        public static implicit operator GameplayTag(string name) => new GameplayTag(name);
    }
}