namespace GoveKits.Network
{
    using System;
    using System.Collections.Generic;
    using GoveKits.Binary;
    using UnityEngine;

    // Generated Code. Do not modify.
    public partial class RelayMessage
    {
        public override int Length()
        {
            int total = base.Length();
            total += BinaryLengthHelper.Get(targetId);
            total += BinaryLengthHelper.Get(InnerMsgID);
            total += BinaryLengthHelper.Get(InnerData);
            total += BinaryLengthHelper.Get(ExcludeIDs);
            return total;
        }

        public override void Writing(byte[] buffer, ref int index)
        {
            base.Writing(buffer, ref index);
            BinaryWriteHelper.Write(buffer, ref index, 10, targetId);
            BinaryWriteHelper.Write(buffer, ref index, 11, InnerMsgID);
            BinaryWriteHelper.Write(buffer, ref index, 12, InnerData);
            BinaryWriteHelper.Write(buffer, ref index, 13, ExcludeIDs);
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
                    case 10: BinaryReadHelper.Read(buffer, ref index, out targetId); break;
                    case 11: BinaryReadHelper.Read(buffer, ref index, out InnerMsgID); break;
                    case 12: BinaryReadHelper.Read(buffer, ref index, out InnerData); break;
                    case 13: BinaryReadHelper.Read(buffer, ref index, out ExcludeIDs); break;
                    default: BinaryReadHelper.Skip(buffer, ref index, type); break;
                }
            }
        }
    }
}
