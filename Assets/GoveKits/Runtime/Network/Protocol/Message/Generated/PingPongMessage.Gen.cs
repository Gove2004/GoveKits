namespace GoveKits.Network
{
    using System;
    using System.Collections.Generic;
    using GoveKits.Binary;
    using UnityEngine;

    // Generated Code. Do not modify.
    public partial class PingPongMessage
    {
        public override int Length()
        {
            int total = base.Length();
            total += BinaryLengthHelper.Get(Timestamp);
            return total;
        }

        public override void Writing(byte[] buffer, ref int index)
        {
            base.Writing(buffer, ref index);
            BinaryWriteHelper.Write(buffer, ref index, 10, Timestamp);
        }

        public override void Reading(byte[] buffer, ref int index, int endPos)
        {
            while (index < endPos)
            {
                if (!BinaryReadHelper.ReadHeader(buffer, ref index, endPos, out ushort tag, out WireType type)) break;
                switch (tag)
                {
                    case 1: BinaryReadHelper.Read(buffer, ref index, out MsgID); break;
                    case 2: BinaryReadHelper.Read(buffer, ref index, out Header); break;
                    case 10: BinaryReadHelper.Read(buffer, ref index, out Timestamp); break;
                    default: BinaryReadHelper.Skip(buffer, ref index, type); break;
                }
            }
        }
    }
}
