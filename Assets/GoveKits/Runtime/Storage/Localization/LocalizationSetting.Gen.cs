namespace GoveKits.Localization
{
    using System;
    using System.Collections.Generic;
    using GoveKits.Binary;
    using UnityEngine;

    // Generated Code. Do not modify.
    public partial class LocalizationSetting : IBinaryData
    {
        public virtual int Length()
        {
            int total = 0;
            total += BinaryLengthHelper.Get(CurrentLanguage);
            return total;
        }

        public virtual void Writing(byte[] buffer, ref int index)
        {
            BinaryWriteHelper.Write(buffer, ref index, 1, CurrentLanguage);
        }

        public virtual void Reading(byte[] buffer, ref int index, int endPos)
        {
            while (index < endPos)
            {
                if (!BinaryReadHelper.ReadHeader(buffer, ref index, endPos, out ushort tag, out WireType type)) break;
                switch (tag)
                {
                    case 1: BinaryReadHelper.Read(buffer, ref index, out CurrentLanguage); break;
                    default: BinaryReadHelper.Skip(buffer, ref index, type); break;
                }
            }
        }
    }
}
