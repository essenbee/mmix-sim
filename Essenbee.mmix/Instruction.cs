using System;

namespace Essenbee.Z80
{
    public class Instruction
    {
        public string Mnemonic { get; set; }
        public Func<byte, byte> Op { get; set; }
        public int Timing { get; set; }

        public Instruction(string mnemonic, Func<byte, byte> op, int timing)
        {
            Mnemonic = mnemonic;
            Op = op;
            Timing = timing;
        }
    }
}
    