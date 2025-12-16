using System;

namespace GoveKits.ECS
{
    public readonly struct Entity : IEquatable<Entity>
    {
        public readonly int ID;
        public readonly int Version; // 新增：用于防止ID复用导致的逻辑错误

        public Entity(int id, int version)
        {
            ID = id;
            Version = version;
        }

        public static Entity Null => new Entity(-1, 0);

        public bool Equals(Entity other) => ID == other.ID && Version == other.Version;
        public override bool Equals(object obj) => obj is Entity other && Equals(other);
        public override int GetHashCode() => ID ^ (Version << 16);
        public override string ToString() => $"Entity({ID}:{Version})";
        
        public static bool operator ==(Entity a, Entity b) => a.Equals(b);
        public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
    }
}