using System;

namespace Essenbee.mmix
{
    public class Instruction
    {
        public string Mnemonic { get; set; }
        public Func<byte, byte> Op { get; set; }
        public Flags InstructionFlags { get; set; }
        public byte SpecialRegInput { get; set; }
        public byte Mems { get; set; }
        public byte Oops { get; set; }

        public Instruction(string mnemonic, Flags flags, byte thirdOperand, byte mems, byte oops, Func<byte, byte> op)
        {
            Mnemonic = mnemonic;
            InstructionFlags = flags;
            SpecialRegInput = thirdOperand;
            Mems = mems;
            Oops = oops;
            Op = op;
        }
    }

    [Flags]
    public enum Flags
    {
        ZImmediate = 1 << 0, // Z is a value rather than a register
        ZSource = 1 << 1,    // Z indicates a register
        YImmediate = 1 << 2, // Y is a value rather than a register
        YSource = 1 << 3,    // Y indicates a register
        XSource = 1 << 4,    // Y indicates a register
        XDest = 1 << 5,
        RelAddress = 1 << 6,
        PushPop = 1 << 7,
    }
}
    