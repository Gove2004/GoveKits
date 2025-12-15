using System;
using System.Collections.Generic;
using GoveKits.Binary;
using UnityEngine;

namespace GoveKits.Network
{
    // Generated Code. Do not modify.
    public partial class HelloMessage
    {
        public override int Length()
        {
            int total = base.Length();
            total += BinaryLengthHelper.Get(PlayerID);
            total += BinaryLengthHelper.Get(Token);
            return total;
        }

        public override void Writing(byte[] buffer, ref int index)
        {
            base.Writing(buffer, ref index);
            BinaryWriteHelper.Write(buffer, ref index, 10, PlayerID);
            BinaryWriteHelper.Write(buffer, ref index, 11, Token);
        }

        public override void Reading(byte[] buffer, ref int index, int endPos)
        {
            while (index < endPos)
            {
                if (!BinaryReadHelper.ReadHeader(buffer, ref index, endPos, out ushort tag, out WireType type)) break;
                switch (tag)
                {
                    case 1: BinaryReadHelper.Read(buffer, ref index, out MsgID); break;
                    case 10: BinaryReadHelper.Read(buffer, ref index, out PlayerID); break;
                    case 11: BinaryReadHelper.Read(buffer, ref index, out Token); break;
                    default: BinaryReadHelper.Skip(buffer, ref index, type); break;
                }
            }
        }
    }
}
