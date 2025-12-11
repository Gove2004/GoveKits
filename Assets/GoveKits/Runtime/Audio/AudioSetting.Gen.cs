namespace GoveKits.Audio
{
    using System;
    using System.Collections.Generic;
    using GoveKits.Binary;
    using UnityEngine;

    // Generated Code. Do not modify.
    public partial class AudioSetting : IBinaryData
    {
        public virtual int Length()
        {
            int total = 0;
            total += BinaryLengthHelper.Get(MasterVolume);
            total += BinaryLengthHelper.Get(BGMVolume);
            total += BinaryLengthHelper.Get(SFXVolume);
            total += BinaryLengthHelper.Get(UIVolume);
            total += BinaryLengthHelper.Get(VoiceVolume);
            return total;
        }

        public virtual void Writing(byte[] buffer, ref int index)
        {
            BinaryWriteHelper.Write(buffer, ref index, 1, MasterVolume);
            BinaryWriteHelper.Write(buffer, ref index, 2, BGMVolume);
            BinaryWriteHelper.Write(buffer, ref index, 3, SFXVolume);
            BinaryWriteHelper.Write(buffer, ref index, 4, UIVolume);
            BinaryWriteHelper.Write(buffer, ref index, 5, VoiceVolume);
        }

        public virtual void Reading(byte[] buffer, ref int index, int endPos)
        {
            while (index < endPos)
            {
                if (!BinaryReadHelper.ReadHeader(buffer, ref index, endPos, out ushort tag, out WireType type)) break;
                switch (tag)
                {
                    case 1: BinaryReadHelper.Read(buffer, ref index, out MasterVolume); break;
                    case 2: BinaryReadHelper.Read(buffer, ref index, out BGMVolume); break;
                    case 3: BinaryReadHelper.Read(buffer, ref index, out SFXVolume); break;
                    case 4: BinaryReadHelper.Read(buffer, ref index, out UIVolume); break;
                    case 5: BinaryReadHelper.Read(buffer, ref index, out VoiceVolume); break;
                    default: BinaryReadHelper.Skip(buffer, ref index, type); break;
                }
            }
        }
    }
}
