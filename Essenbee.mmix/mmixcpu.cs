using Essenbee.mmix;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Essenbee.mmix
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
        public MemNode MemoryRoot { get; private set; } // Root of simulated Memory
        public Dictionary<byte, Instruction> Instructions { get; } = new Dictionary<byte, Instruction>();

        private IBus _bus = null!;
        private long _absoluteAddress = 0x0000000000000000;
        private byte _currentOpCode = 0x00;
        private int _clockCycles = 0;
        private uint _priority = 314159265; // Pseudorandom time stamp counter
        private MemNode _lastAccessed;      // Memory node most recently accessed

        public enum R
        {
            B, D, E, H, J, M, R, BB,
            C, N, O, S, I, T, TT, K,
            Q, U, V, G, L, A, F, P,
            W, X, Y, Z, WW, XX, YY, ZZ,
        };

        public mmixcpu()
        {
            // Allocate a chunk of simulated memory for the Pool Segment
            MemoryRoot = new MemNode(0x40000000_00000000, ref _priority);
            _lastAccessed = MemoryRoot;

            Instructions = new Dictionary<byte, Instruction>
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
                // Load-Store Instructions
                { 0x80, new Instruction ( "LDB", (Flags)0x2a, 0, 1, 1, UNDEF ) },
                { 0x81, new Instruction ( "LDBI", (Flags)0x29, 0, 1, 1, UNDEF ) },
                { 0x82, new Instruction ( "LDBU", (Flags)0x2a, 0, 1, 1, UNDEF ) },
                { 0x83, new Instruction ( "LDBUI", (Flags)0x29, 0, 1, 1, UNDEF ) },
                { 0x84, new Instruction ( "LDW", (Flags)0x2a, 0, 1, 1, UNDEF ) },
                { 0x85, new Instruction ( "LDWI", (Flags)0x29, 0, 1, 1, UNDEF ) },
                { 0x86, new Instruction ( "LDWU", (Flags)0x2a, 0, 1, 1, UNDEF ) },
                { 0x87, new Instruction ( "LDWUI", (Flags)0x29, 0, 1, 1, UNDEF ) },
                { 0x88, new Instruction ( "LDT", (Flags)0x2a, 0, 1, 1, UNDEF ) },
                { 0x89, new Instruction ( "LDTI", (Flags)0x29, 0, 1, 1, UNDEF ) },
                { 0x8A, new Instruction ( "LDTU", (Flags)0x2a, 0, 1, 1, UNDEF ) },
                { 0x8B, new Instruction ( "LDTUI", (Flags)0x29, 0, 1, 1, UNDEF ) },
                { 0x8C, new Instruction ( "LDO", (Flags)0x2a, 0, 1, 1, UNDEF ) },
                { 0x8D, new Instruction ( "LDOI", (Flags)0x29, 0, 1, 1, UNDEF ) },
                { 0x8E, new Instruction ( "LDOU", (Flags)0x2a, 0, 1, 1, UNDEF ) },
                { 0x8F, new Instruction ( "LDOUI", (Flags)0x29, 0, 1, 1, UNDEF ) },
                { 0x90, new Instruction ( "LDSF", (Flags)0x2a, 0, 1, 1, UNDEF ) },
                { 0x91, new Instruction ( "LDSFI", (Flags)0x29, 0, 1, 1, UNDEF ) },
                { 0x92, new Instruction ( "LDHT", (Flags)0x2a, 0, 1, 1, UNDEF ) },
                { 0x93, new Instruction ( "LDHTI", (Flags)0x29, 0, 1, 1, UNDEF ) },
                { 0x94, new Instruction ( "CSWAP", (Flags)0x3a, 0, 2, 2, UNDEF ) },
                { 0x95, new Instruction ( "CSWAPI", (Flags)0x39, 0, 2, 2, UNDEF ) },
                { 0x96, new Instruction ( "LDUNC", (Flags)0x2a, 0, 1, 1, UNDEF ) },
                { 0x97, new Instruction ( "LDUNCI", (Flags)0x29, 0, 1, 1, UNDEF ) },
                { 0x98, new Instruction ( "LDVTS", (Flags)0x2a, 0, 0, 1, UNDEF ) },
                { 0x99, new Instruction ( "LDVTSI", (Flags)0x29, 0, 0, 1, UNDEF ) },
                { 0x9A, new Instruction ( "PRELD", (Flags)0x0a, 0, 0, 1, UNDEF ) },
                { 0x9B, new Instruction ( "PRELDI", (Flags)0x09, 0, 0, 1, UNDEF ) },
                { 0x9C, new Instruction ( "PREGO", (Flags)0x0a, 0, 0, 1, UNDEF ) },
                { 0x9D, new Instruction ( "PREGOI", (Flags)0x09, 0, 0, 1, UNDEF ) },
                { 0x9E, new Instruction ( "GO", (Flags)0x2a, 0, 0, 3, UNDEF ) },
                { 0x9F, new Instruction ( "GOI", (Flags)0x29, 0, 0, 3, UNDEF ) },
                { 0xA0, new Instruction ( "STB", (Flags)0x1a, 0, 1, 1, UNDEF ) },
                { 0xA1, new Instruction ( "STBI", (Flags)0x19, 0, 1, 1, UNDEF ) },
                { 0xA2, new Instruction ( "STBU", (Flags)0x1a, 0, 1, 1, UNDEF ) },
                { 0xA3, new Instruction ( "STBUI", (Flags)0x19, 0, 1, 1, UNDEF ) },
                { 0xA4, new Instruction ( "STW", (Flags)0x1a, 0, 1, 1, UNDEF ) },
                { 0xA5, new Instruction ( "STWI", (Flags)0x19, 0, 1, 1, UNDEF ) },
                { 0xA6, new Instruction ( "STWU", (Flags)0x1a, 0, 1, 1, UNDEF ) },
                { 0xA7, new Instruction ( "STWUI", (Flags)0x19, 0, 1, 1, UNDEF ) },
                { 0xA8, new Instruction ( "STT", (Flags)0x1a, 0, 1, 1, UNDEF ) },
                { 0xA9, new Instruction ( "STTI", (Flags)0x19, 0, 1, 1, UNDEF ) },
                { 0xAA, new Instruction ( "STTU", (Flags)0x1a, 0, 1, 1, UNDEF ) },
                { 0xAB, new Instruction ( "STTUI", (Flags)0x19, 0, 1, 1, UNDEF ) },
                { 0xAC, new Instruction ( "STO", (Flags)0x1a, 0, 1, 1, UNDEF ) },
                { 0xAD, new Instruction ( "STOI", (Flags)0x19, 0, 1, 1, UNDEF ) },
                { 0xAE, new Instruction ( "STOU", (Flags)0x1a, 0, 1, 1, UNDEF ) },
                { 0xAF, new Instruction ( "STOUI", (Flags)0x19, 0, 1, 1, UNDEF ) },
                { 0xB0, new Instruction ( "STSF", (Flags)0x1a, 0, 1, 1, UNDEF ) },
                { 0xB1, new Instruction ( "STSFI", (Flags)0x19, 0, 1, 1, UNDEF ) },
                { 0xB2, new Instruction ( "STHT", (Flags)0x1a, 0, 1, 1, UNDEF ) },
                { 0xB3, new Instruction ( "STHTI", (Flags)0x19, 0, 1, 1, UNDEF ) },
                { 0xB4, new Instruction ( "STCO", (Flags)0x0a, 0, 1, 1, UNDEF ) },
                { 0xB5, new Instruction ( "STCOI", (Flags)0x09, 0, 1, 1, UNDEF ) },
                { 0xB6, new Instruction ( "STUNC", (Flags)0x1a, 0, 1, 1, UNDEF ) },
                { 0xB7, new Instruction ( "STUNCI", (Flags)0x19, 0, 1, 1, UNDEF ) },
                { 0xB8, new Instruction ( "SYNCD", (Flags)0x0a, 0, 0, 1, UNDEF ) },
                { 0xB9, new Instruction ( "SYNCDI", (Flags)0x09, 0, 0, 1, UNDEF ) },
                { 0xBA, new Instruction ( "PREST", (Flags)0x0a, 0, 0, 1, UNDEF ) },
                { 0xBB, new Instruction ( "PRESTI", (Flags)0x09, 0, 0, 1, UNDEF ) },
                { 0xBC, new Instruction ( "SYNCID", (Flags)0x0a, 0, 0, 1, UNDEF ) },
                { 0xBD, new Instruction ( "SYNCIDI", (Flags)0x09, 0, 0, 1, UNDEF ) },
                { 0xBE, new Instruction ( "PUSHGO", (Flags)0xaa, 0, 0, 3, UNDEF ) },
                { 0xBF, new Instruction ( "PUSHGOI", (Flags)0xa9, 0, 0, 3, UNDEF ) }, 
                // Logic and Control Instructions
                { 0xC0, new Instruction ( "OR",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xC1, new Instruction ( "ORI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xC2, new Instruction ( "ORN",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xC3, new Instruction ( "ORNI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xC4, new Instruction ( "NOR",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xC5, new Instruction ( "NORI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xC6, new Instruction ( "XOR",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xC7, new Instruction ( "XORI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xC8, new Instruction ( "AND",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xC9, new Instruction ( "ANDI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xCA, new Instruction ( "ANDN",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xCB, new Instruction ( "ANDNI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xCC, new Instruction ( "NAND",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xCD, new Instruction ( "NANDI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xCE, new Instruction ( "NXOR",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xCF, new Instruction ( "NXORI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xD0, new Instruction ( "BDIF",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xD1, new Instruction ( "BDIFI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xD2, new Instruction ( "WDIF",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xD3, new Instruction ( "WDIFI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xD4, new Instruction ( "TDIF",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xD5, new Instruction ( "TDIFI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xD6, new Instruction ( "ODIF",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xD7, new Instruction ( "ODIFI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xD8, new Instruction ( "MUX",(Flags)0x2a,(int)R.M,0,1,UNDEF ) },
                { 0xD9, new Instruction ( "MUXI",(Flags)0x29,(int)R.M,0,1,UNDEF ) },
                { 0xDA, new Instruction ( "SADD",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xDB, new Instruction ( "SADDI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xDC, new Instruction ( "MOR",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xDD, new Instruction ( "MORI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xDE, new Instruction ( "MXOR",(Flags)0x2a,0,0,1,UNDEF ) },
                { 0xDF, new Instruction ( "MXORI",(Flags)0x29,0,0,1,UNDEF ) },
                { 0xE0, new Instruction ( "SETH",(Flags)0x20,0,0,1,UNDEF ) },
                { 0xE1, new Instruction ( "SETMH",(Flags)0x20,0,0,1,UNDEF ) },
                { 0xE2, new Instruction ( "SETML",(Flags)0x20,0,0,1,UNDEF ) },
                { 0xE3, new Instruction ( "SETL",(Flags)0x20,0,0,1,UNDEF ) },
                { 0xE4, new Instruction ( "INCH",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xE5, new Instruction ( "INCMH",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xE6, new Instruction ( "INCML",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xE7, new Instruction ( "INCL",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xE8, new Instruction ( "ORH",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xE9, new Instruction ( "ORMH",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xEA, new Instruction ( "ORML",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xEB, new Instruction ( "ORL",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xEC, new Instruction ( "ANDNH",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xED, new Instruction ( "ANDNMH",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xEE, new Instruction ( "ANDNML",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xEF, new Instruction ( "ANDNL",(Flags)0x30,0,0,1,UNDEF ) },
                { 0xF0, new Instruction ( "JMP",(Flags)0x40,0,0,1,UNDEF ) },
                { 0xF1, new Instruction ( "JMPB",(Flags)0x40,0,0,1,UNDEF ) },
                { 0xF2, new Instruction ( "PUSHJ",(Flags)0xe0,0,0,1,UNDEF ) },
                { 0xF3, new Instruction ( "PUSHJB",(Flags)0xe0,0,0,1,UNDEF ) },
                { 0xF4, new Instruction ( "GETA",(Flags)0x60,0,0,1,UNDEF ) },
                { 0xF5, new Instruction ( "GETAB",(Flags)0x60,0,0,1,UNDEF ) },
                { 0xF6, new Instruction ( "PUT",(Flags)0x02,0,0,1,UNDEF ) },
                { 0xF7, new Instruction ( "PUTI",(Flags)0x01,0,0,1,UNDEF ) },
                { 0xF8, new Instruction ( "POP",(Flags)0x80,(int)R.J,0,3,UNDEF ) },
                { 0xF9, new Instruction ( "RESUME",(Flags)0x00,0,0,5,UNDEF ) },
                { 0xFA, new Instruction ( "SAVE",(Flags)0x20,0,20,1,UNDEF ) },
                { 0xFB, new Instruction ( "UNSAVE",(Flags)0x82,0,20,1,UNDEF ) },
                { 0xFC, new Instruction ( "SYNC",(Flags)0x01,0,0,1,UNDEF ) },
                { 0xFD, new Instruction ( "SWYM",(Flags)0x00,0,0,1,UNDEF ) },
                { 0xFE, new Instruction ( "GET",(Flags)0x20,0,0,1,UNDEF ) },
                { 0xFF, new Instruction ( "TRIP",(Flags)0x0a,255,0,5,UNDEF ) },
            };
        }

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

        public MemTetra FindMemory(ulong address)
        {
            var key = address & 0xFFFFFFFF_FFFFF800;    // MemNode location as key
            var offset = address & 0x00000000_000007FC; // Position of tetra-byte within MemNode

            if (_lastAccessed.Location != key)
            {
                // Search for key in the tree, setting lastAccessed to its location
                if (MemoryRoot.Location == key)
                {
                    _lastAccessed = MemoryRoot;
                }
                else
                {
                    _lastAccessed = SearchTree(MemoryRoot, key);
                }
            }

            return _lastAccessed.Memory[offset >> 2]; // Return tetra-byte found
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

            //  opCode = opCode.Replace("+d", $"{d.ToString("+0;-(Flags)0x", c)}", StringComparison.InvariantCulture);
            //  opCode = opCode.Replace("e", $"${e.ToString("+0;-(Flags)0x", c)}", StringComparison.InvariantCulture);
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

        private MemNode SearchTree(MemNode memNode, ulong key)
        {
            int result;

            // Walk the tree to find the correct MemNode...
            while (memNode != null)
            {
                result = key.CompareTo(memNode.Location);
                if (result == 0) return memNode;

                if (result < 0)
                {
                    memNode = memNode.Left;
                }
                else
                {
                    memNode = memNode.Right;
                }
            }

            // Not found, so insert the missing node...
            var newMemNode = new MemNode(key, ref _priority);
            _ = MemNode.InsertMemNode(newMemNode, MemoryRoot);

            return newMemNode;
        }

        private (byte opCode, Instruction operation) GetInstruction(byte code) => (code, Instructions[code]);
    }
}
