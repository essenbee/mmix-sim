using System.Collections.Generic;
using System.Globalization;

namespace Essenbee.Z80
{
    public partial class mmixcpu
    {
        // ========================================
        // General Purpose Registers
        // ========================================
        public long[] Register { get; set; } = new long[265];

        // ========================================
        // Special Purpose Registers
        // ========================================
        // [0] = rB  Bootstrap (TRIP) Register 
        // [1] = rD  Dividend Register
        // [2] = rE  Epsilon Register
        // [3] = rH  Hi-mult Register
        // [4] = rJ  Return-Jump Register
        // [5] = rM  Multiplex Mask Register
        // [6] = rR  Remainder Register
        // [7] = rBB  Bootstrap (TRAP) Register
        // [8] = rC  Continuation Register
        // [9] = rN  Serial Number Register 
        // [10] = rO  Register Stack Offset
        // [11] = rS  Stack Pointer
        // [12] = rI  Interval Counter
        // [13] = rT  Trap Address Register
        // [14] = rTT  Dynamic Trap Address Register
        // [15] = rK  Interrupt Mask
        // [16] = rQ  Interrupt Request Register
        // [17] = rU  Usage Register
        // [18] = rV  Virtual Translation Register
        // [19] = rG  Global Threshold Register
        // [20] = rL  Local Threshold Register
        // [21] = rA  Arithmetic Status Register
        // [22] = rF  Failure Location Register
        // [23] = rP  Prediction Register
        // [24] = rW  Where Interrupted Register (TRIP)
        // [25] = rX  Execution Register (TRIP)
        // [26] = rY  Y Operand (TRIP)
        // [27] = rZ  Z Operand (TRIP)
        // [28] = rWW  Where Interrupted Register (TRAP) 
        // [29] = rXX  Execution Register (TRAP)
        // [30] = rYY  Y Operand (TRAP)
        // [31] = rZZ  Z Operand (TRAP)
        public long[] SpecialRegister { get; set; } = new long[32];

        public Dictionary<byte, Instruction> Instructions { get; } = new Dictionary<byte, Instruction>();

        private IBus _bus = null!;
        private long _absoluteAddress = 0x0000000000000000;
        private byte _currentOpCode = 0x00;
        private int _clockCycles = 0;

        public enum R
        {
            B, D, E, H, J, M, R, BB,
            C, N, O, S, I, T, TT, K,
            Q, U, V, G, L, A, F, P,
            W, X, Y, Z, WW, XX, YY, ZZ,
        };

        public mmixcpu() => Instructions = new Dictionary<byte, Instruction>
        {
          // Arithmetic Instructions
          { 0x00, new Instruction ( "TRAP", (Flags)0x0a, 255, 0, 5, UNDEF ) },
          { 0x01, new Instruction ( "FCMP", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x02, new Instruction ( "FUN", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x03, new Instruction ( "FEQL", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x04, new Instruction ( "FADD", (Flags)0x2a, 0, 0, 4, UNDEF ) },
          { 0x05, new Instruction ( "FIX", (Flags)0x26, 0, 0, 4, UNDEF ) },
          { 0x06, new Instruction ( "FSUB", (Flags)0x2a, 0, 0, 4, UNDEF ) },
          { 0x07, new Instruction ( "FIXU", (Flags)0x26, 0, 0, 4, UNDEF ) },
          { 0x08, new Instruction ( "FLOT", (Flags)0x26, 0, 0, 4, UNDEF ) },
          { 0x09, new Instruction ( "FLOTI", (Flags)0x25, 0, 0, 4, UNDEF ) },
          { 0x0A, new Instruction ( "FLOTU", (Flags)0x26, 0, 0, 4, UNDEF ) },
          { 0x0B, new Instruction ( "FLOTUI", (Flags)0x25, 0, 0, 4, UNDEF ) },
          { 0x0C, new Instruction ( "SFLOT", (Flags)0x26, 0, 0, 4, UNDEF ) },
          { 0x0D, new Instruction ( "SFLOTI", (Flags)0x25, 0, 0, 4, UNDEF ) },
          { 0x0E, new Instruction ( "SFLOTU", (Flags)0x26, 0, 0, 4, UNDEF ) },
          { 0x0F, new Instruction ( "SFLOTUI", (Flags)0x25, 0, 0, 4, UNDEF ) },
          { 0x10, new Instruction ( "FMUL", (Flags)0x2a, 0, 0, 4, UNDEF ) },
          { 0x11, new Instruction ( "FCMPE", (Flags)0x2a, (int)R.E, 0, 4, UNDEF ) },
          { 0x12, new Instruction ( "FUNE", (Flags)0x2a, (int)R.E, 0, 1, UNDEF ) },
          { 0x13, new Instruction ( "FEQLE", (Flags)0x2a, (int)R.E, 0, 4, UNDEF ) },
          { 0x14, new Instruction ( "FDIV", (Flags)0x2a, 0, 0, 40, UNDEF ) },
          { 0x15, new Instruction ( "FSQRT", (Flags)0x26, 0, 0, 40, UNDEF ) },
          { 0x16, new Instruction ( "FREM", (Flags)0x2a, 0, 0, 4, UNDEF ) },
          { 0x17, new Instruction ( "FINT", (Flags)0x26, 0, 0, 4, UNDEF ) },
          { 0x18, new Instruction ( "MUL", (Flags)0x2a, 0, 0, 10, UNDEF ) },
          { 0x19, new Instruction ( "MULI", (Flags)0x29, 0, 0, 10, UNDEF ) },
          { 0x1A, new Instruction ( "MULU", (Flags)0x2a, 0, 0, 10, UNDEF ) },
          { 0x1B, new Instruction ( "MULUI", (Flags)0x29, 0, 0, 10, UNDEF ) },
          { 0x1C, new Instruction ( "DIV", (Flags)0x2a, 0, 0, 60, UNDEF ) },
          { 0x1D, new Instruction ( "DIVI", (Flags)0x29, 0, 0, 60, UNDEF ) },
          { 0x1E, new Instruction ( "DIVU", (Flags)0x2a, (int)R.D, 0, 60, UNDEF ) },
          { 0x1F, new Instruction ( "DIVUI", (Flags)0x29, (int)R.D, 0, 60, UNDEF ) },
          { 0x20, new Instruction ( "ADD", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x21, new Instruction ( "ADDI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x22, new Instruction ( "ADDU", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x23, new Instruction ( "ADDUI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x24, new Instruction ( "SUB", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x25, new Instruction ( "SUBI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x26, new Instruction ( "SUBU", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x27, new Instruction ( "SUBUI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x28, new Instruction ( "2ADDU", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x29, new Instruction ( "2ADDUI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x2A, new Instruction ( "4ADDU", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x2B, new Instruction ( "4ADDUI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x2C, new Instruction ( "8ADDU", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x2D, new Instruction ( "8ADDUI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x2E, new Instruction ( "16ADDU", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x2F, new Instruction ( "16ADDUI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x30, new Instruction ( "CMP", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x31, new Instruction ( "CMPI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x32, new Instruction ( "CMPU", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x33, new Instruction ( "CMPUI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x34, new Instruction ( "NEG", (Flags)0x26, 0, 0, 1, UNDEF ) },
          { 0x35, new Instruction ( "NEGI", (Flags)0x25, 0, 0, 1, UNDEF ) },
          { 0x36, new Instruction ( "NEGU", (Flags)0x26, 0, 0, 1, UNDEF ) },
          { 0x37, new Instruction ( "NEGUI", (Flags)0x25, 0, 0, 1, UNDEF ) },
          { 0x38, new Instruction ( "SL", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x39, new Instruction ( "SLI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x3A, new Instruction ( "SLU", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x3B, new Instruction ( "SLUI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x3C, new Instruction ( "SR", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x3D, new Instruction ( "SRI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x3E, new Instruction ( "SRU", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x3F, new Instruction ( "SRUI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          // Branching Instructions
          { 0x40, new Instruction ( "BN", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x41, new Instruction ( "BNB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x42, new Instruction ( "BZ", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x43, new Instruction ( "BZB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x44, new Instruction ( "BP", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x45, new Instruction ( "BPB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x46, new Instruction ( "BOD", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x47, new Instruction ( "BODB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x48, new Instruction ( "BNN", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x49, new Instruction ( "BNNB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x4A, new Instruction ( "BNZ", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x4B, new Instruction ( "BNZB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x4C, new Instruction ( "BNP", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x4D, new Instruction ( "BNPB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x4E, new Instruction ( "BEV", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x4F, new Instruction ( "BEVB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x50, new Instruction ( "PBN", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x51, new Instruction ( "PBNB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x52, new Instruction ( "PBZ", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x53, new Instruction ( "PBZB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x54, new Instruction ( "PBP", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x55, new Instruction ( "PBPB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x56, new Instruction ( "PBOD", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x57, new Instruction ( "PBODB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x58, new Instruction ( "PBNN", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x59, new Instruction ( "PBNNB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x5A, new Instruction ( "PBNZ", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x5B, new Instruction ( "PBNZB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x5C, new Instruction ( "PBNP", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x5D, new Instruction ( "PBNPB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x5E, new Instruction ( "PBEV", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x5F, new Instruction ( "PBEVB", (Flags)0x50, 0, 0, 1, UNDEF ) },
          { 0x60, new Instruction ( "CSN", (Flags)0x3a, 0, 0, 1, UNDEF ) },
          { 0x61, new Instruction ( "CSNI", (Flags)0x39, 0, 0, 1, UNDEF ) },
          { 0x62, new Instruction ( "CSZ", (Flags)0x3a, 0, 0, 1, UNDEF ) },
          { 0x63, new Instruction ( "CSZI", (Flags)0x39, 0, 0, 1, UNDEF ) },
          { 0x64, new Instruction ( "CSP", (Flags)0x3a, 0, 0, 1, UNDEF ) },
          { 0x65, new Instruction ( "CSPI", (Flags)0x39, 0, 0, 1, UNDEF ) },
          { 0x66, new Instruction ( "CSOD", (Flags)0x3a, 0, 0, 1, UNDEF ) },
          { 0x67, new Instruction ( "CSODI", (Flags)0x39, 0, 0, 1, UNDEF ) },
          { 0x68, new Instruction ( "CSNN", (Flags)0x3a, 0, 0, 1, UNDEF ) },
          { 0x69, new Instruction ( "CSNNI", (Flags)0x39, 0, 0, 1, UNDEF ) },
          { 0x6A, new Instruction ( "CSNZ", (Flags)0x3a, 0, 0, 1, UNDEF ) },
          { 0x6B, new Instruction ( "CSNZI", (Flags)0x39, 0, 0, 1, UNDEF ) },
          { 0x6C, new Instruction ( "CSNP", (Flags)0x3a, 0, 0, 1, UNDEF ) },
          { 0x6D, new Instruction ( "CSNPI", (Flags)0x39, 0, 0, 1, UNDEF ) },
          { 0x6E, new Instruction ( "CSEV", (Flags)0x3a, 0, 0, 1, UNDEF ) },
          { 0x6F, new Instruction ( "CSEVI", (Flags)0x39, 0, 0, 1, UNDEF ) },
          { 0x70, new Instruction ( "ZSN", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x71, new Instruction ( "ZSNI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x72, new Instruction ( "ZSZ", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x73, new Instruction ( "ZSZI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x74, new Instruction ( "ZSP", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x75, new Instruction ( "ZSPI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x76, new Instruction ( "ZSOD", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x77, new Instruction ( "ZSODI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x78, new Instruction ( "ZSNN", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x79, new Instruction ( "ZSNNI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x7A, new Instruction ( "ZSNZ", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x7B, new Instruction ( "ZSNZI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x7C, new Instruction ( "ZSNP", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x7D, new Instruction ( "ZSNPI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          { 0x7E, new Instruction ( "ZSEV", (Flags)0x2a, 0, 0, 1, UNDEF ) },
          { 0x7F, new Instruction ( "ZSEVI", (Flags)0x29, 0, 0, 1, UNDEF ) },
          // Load/Store Instructions

          // Logic and Control Instructions
        };

        public void ConnectToBus(IBus bus) => _bus = bus;

        public void Reset(bool hardReset = false)
        {

        }

        public void Step()
        {
            //var address = PC;
            //var (_, operation) = PeekNextInstruction(ReadFromBus(address), ref address);
            //var tStates = operation.TStates;

            //for (int i = 0; i < tStates; i++)
            //{
            //  Tick();
            //}
        }

        public void Interrupt()
        {

        }

        public Dictionary<long, string> Disassemble(long start, long end)
        {
            var address = start;
            var retVal = new Dictionary<long, string>();
            var culture = new CultureInfo("en-US");

            while (address <= end)
            {
                var (addr, op, nextAddr) = DisassembleInstruction(address, culture);
                address = nextAddr;
                retVal.Add(addr, op);
            }

            return retVal;
        }

        public bool IsOpCodeSupported(string opCode)
        {
            var c = new CultureInfo("en-US");

            if (string.IsNullOrWhiteSpace(opCode) || !int.TryParse(opCode, NumberStyles.HexNumber, c, out var _))
            {
                return false;
            }

            return Instructions.ContainsKey(byte.Parse(opCode, NumberStyles.HexNumber, c));
        }

        private void Tick()
        {
            if (_clockCycles == 0)
            {
                //var address = PC;
                //_currentOpCode = ReadFromBus(address);
                //var (opCode, operation) = FetchNextInstruction(_currentOpCode, ref address);
                //PC = address;
                //_currentOpCode = opCode;
                //_clockCycles = operation.TStates;
                //operation.Op(_currentOpCode);
            }

            _clockCycles--;
        }

        private byte ReadFromBus(long addr) => _bus.Read(addr, false);
        private void WriteToBus(long addr, byte data) => _bus.Write(addr, data);
        private byte Fetch(Dictionary<byte, Instruction> lookupTable) =>
            //lookupTable[_currentOpCode].AddressingMode1();
            ReadFromBus(_absoluteAddress);

        private (byte opCode, Instruction operation) PeekNextInstruction(byte code, ref long address) =>
          FetchNextInstruction(code, ref address);

        private (byte opCode, Instruction operation) FetchNextInstruction(byte code, ref long address) => GetInstruction(code);

        private (long opAddress, string opString, long nextAddress) DisassembleInstruction(long address, CultureInfo c)
        {
            var opAddress = address;
            var aByte = ReadFromBus(address++);
            Instruction operation;

            (_, operation) = GetInstruction(aByte);

            var opCode = $"{operation.Mnemonic}";

            // Operands
            //if (operation.AddressingMode1 == IMM)
            //{
            //  var n = ReadFromBus(address++).ToString("X2", c);
            //  opCode = opCode.Replace("n", $"&{n}", StringComparison.InvariantCulture);
            //}
            //else if (operation.AddressingMode1 == REL)
            //{
            //  var d = (sbyte)ReadFromBus(address++);
            //  var e = d > 0 ? d + 2 : d - 2;

            //  opCode = opCode.Replace("+d", $"{d.ToString("+0;-#", c)}", StringComparison.InvariantCulture);
            //  opCode = opCode.Replace("e", $"${e.ToString("+0;-#", c)}", StringComparison.InvariantCulture);
            //}
            //else if (operation.AddressingMode1 == IMX)
            //{
            //  var loByte = ReadFromBus(address++);
            //  var hiByte = (ushort)ReadFromBus(address++);
            //  var val = (ushort)((hiByte << 8) + loByte);
            //  var nn = val.ToString("X4", c);
            //  opCode = opCode.Replace("nn", $"&{nn}", StringComparison.InvariantCulture);
            //}

            return (opAddress, opCode, address);
        }

        private (byte opCode, Instruction operation) GetInstruction(byte code) => (code, Instructions[code]);
    }
}
