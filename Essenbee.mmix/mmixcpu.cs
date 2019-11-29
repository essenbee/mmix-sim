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
        // [0]  = rB    Bootstrap (TRIP) Register 
        // [1]  = rD    Dividend Register
        // [2]  = rE    Epsilon Register
        // [3]  = rH    Hi-mult Register
        // [4]  = rJ    Return-Jump Register
        // [5]  = rM    Multiplex Mask Register
        // [6]  = rR    Remainder Register
        // [7]  = rBB   Bootstrap (TRAP) Register
        // [8]  = rC    Continuation Register
        // [9]  = rN    Serial Number Register 
        // [10] = rO    Register Stack Offset
        // [11] = rS    Stack Pointer
        // [12] = rI    Interval Counter
        // [13] = rT    Trap Address Register
        // [14] = rTT   Dynamic Trap Address Register
        // [15] = rK    Interrupt Mask
        // [16] = rQ    Interrupt Request Register
        // [17] = rU    Usage Register
        // [18] = rV    Virtual Translation Register
        // [19] = rG    Global Threshold Register
        // [20] = rL    Local Threshold Register
        // [21] = rA    Arithmetic Status Register
        // [22] = rF    Failure Location Register
        // [23] = rP    Prediction Register
        // [24] = rW    Where Interrupted Register (TRIP)
        // [25] = rX    Execution Register (TRIP)
        // [26] = rY    Y Operand (TRIP)
        // [27] = rZ    Z Operand (TRIP)
        // [28] = rWW   Where Interrupted Register (TRAP) 
        // [29] = rXX   Execution Register (TRAP)
        // [30] = rYY   Y Operand (TRAP)
        // [31] = rZZ   Z Operand (TRAP)
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
            //    Tick();
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
            //    var n = ReadFromBus(address++).ToString("X2", c);
            //    opCode = opCode.Replace("n", $"&{n}", StringComparison.InvariantCulture);
            //}
            //else if (operation.AddressingMode1 == REL)
            //{
            //    var d = (sbyte)ReadFromBus(address++);
            //    var e = d > 0 ? d + 2 : d - 2;

            //    opCode = opCode.Replace("+d", $"{d.ToString("+0;-#", c)}", StringComparison.InvariantCulture);
            //    opCode = opCode.Replace("e", $"${e.ToString("+0;-#", c)}", StringComparison.InvariantCulture);
            //}
            //else if (operation.AddressingMode1 == IMX)
            //{
            //    var loByte = ReadFromBus(address++);
            //    var hiByte = (ushort)ReadFromBus(address++);
            //    var val = (ushort)((hiByte << 8) + loByte);
            //    var nn = val.ToString("X4", c);
            //    opCode = opCode.Replace("nn", $"&{nn}", StringComparison.InvariantCulture);
            //}

            return (opAddress, opCode, address);
        }

        private (byte opCode, Instruction operation) GetInstruction(byte code) => (code, Instructions[code]);
    }
}
