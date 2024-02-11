using System;
using System.IO;

namespace GvasFormat.Serialization.UETypes {
    public sealed class UEUInt32Property : UEProperty {
        public UEUInt32Property() { }

        public UEUInt32Property(BinaryReader reader, long valueLength) {
            var terminator = reader.ReadByte();
            if (terminator != 0)
                throw new FormatException($"Offset: {reader.BaseStream.Position - 1:x8}. Expected terminator (0x00), but was (0x{terminator:x2})");

            Value = reader.ReadUInt32();            
        }

        public override void Serialize(BinaryWriter writer) {
            throw new System.NotImplementedException();
        }

        public uint Value;
    }
}