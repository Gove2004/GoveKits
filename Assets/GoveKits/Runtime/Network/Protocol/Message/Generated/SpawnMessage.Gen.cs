using System;
using System.Collections.Generic;
using GoveKits.Binary;
using UnityEngine;

namespace GoveKits.Network
{
    // Generated Code. Do not modify.
    public partial class SpawnMessage
    {
        public override int Length()
        {
            int total = base.Length();
            total += BinaryLengthHelper.Get(PrefabName);
            total += BinaryLengthHelper.Get(NetID);
            total += BinaryLengthHelper.Get(OwnerID);
            total += BinaryLengthHelper.Get(Pos);
            total += BinaryLengthHelper.Get(Rot);
            return total;
        }

        public override void Writing(byte[] buffer, ref int index)
        {
            base.Writing(buffer, ref index);
            BinaryWriteHelper.Write(buffer, ref index, 10, PrefabName);
            BinaryWriteHelper.Write(buffer, ref index, 11, NetID);
            BinaryWriteHelper.Write(buffer, ref index, 12, OwnerID);
            BinaryWriteHelper.Write(buffer, ref index, 13, Pos);
            BinaryWriteHelper.Write(buffer, ref index, 14, Rot);
        }

        public override void Reading(byte[] buffer, ref int index, int endPos)
        {
            while (index < endPos)
            {
                if (!BinaryReadHelper.ReadHeader(buffer, ref index, endPos, out ushort tag, out WireType type)) break;
                switch (tag)
                {
                    case 1: BinaryReadHelper.Read(buffer, ref index, out MsgID); break;
                    case 10: BinaryReadHelper.Read(buffer, ref index, out PrefabName); break;
                    case 11: BinaryReadHelper.Read(buffer, ref index, out NetID); break;
                    case 12: BinaryReadHelper.Read(buffer, ref index, out OwnerID); break;
                    case 13: BinaryReadHelper.Read(buffer, ref index, out Pos); break;
                    case 14: BinaryReadHelper.Read(buffer, ref index, out Rot); break;
                    default: BinaryReadHelper.Skip(buffer, ref index, type); break;
                }
            }
        }
    }
}
