using System;
using System.IO;

namespace GvasFormat.Serialization.UETypes {
    public sealed class UEUInt16Property : UEProperty {
        public UEUInt16Property() { }

        public UEUInt16Property(BinaryReader reader, long valueLength) {
            var terminator = reader.ReadByte();
            if (terminator != 0)
                throw new FormatException($"Offset: {reader.BaseStream.Position - 1:x8}. Expected terminator (0x00), but was (0x{terminator:x2})");

            Value = reader.ReadUInt16();            
        }

        public override void Serialize(BinaryWriter writer) {
            throw new NotImplementedException();
        }

        public uint Value;
    }
}