using System;

namespace Essenbee.Z80
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
        ZImmediate = 1 << 0,
        ZSource = 1 << 1,
        YImmediate = 1 << 2,
        YSource = 1 << 3,
        XSource = 1 << 4,
        XDest= 1 << 5,
        RelAddress = 1 << 6,
        PushPop = 1 << 7,
    }
}
    