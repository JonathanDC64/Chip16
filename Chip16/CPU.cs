using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace Chip16
{
    /// <summary>
    /// Registers:
    /// Chip16 uses 16 bit words, as the name implies. Hence the registers are 16 bits wide, and memory is read 16 bits at a time.
    /// Chip16 is a little-endian machine(LSB, Least Significant Byte first). All values in registers and memory are read and written in this representation.
    /// 
    /// Instructions:
    /// Chip16 operates at a frequency of 1 MHz(1,000,000 cycles/second).
    /// All instructions:
    /// take 1 cycle to execute
    /// are stored in 4 bytes
    ///
    /// </summary>
    public class CPU
    {
        [DataContract]
        public class Instruction
        {
            [DataMember]
            public byte Opcode { get; set; }

            [DataMember]
            public string Mnemonic { get; set; }

            [DataMember]
            public List<string> Flags { get; set; }

            //public HashSet<Flags> Flags;

            public Action Operation { get; set; }

            public void Execute()
            {
                this.Operation();
            }
        }

        // 16 bit program counter
        public UInt16 PC;

        // 16 bit stack pointer
        private UInt16 SP;

        // 16x 16 bit general purpose registers (R0..RF)
        private readonly UInt16[] R;

        // 8 bit flag register
        private class FLAGS
        {
            public bool C = false;
            public bool Z = false;
            public bool O = false;
            public bool N = false;

            public byte Bits
            {
                get => (byte)((Convert.ToByte(C) << 1) | (Convert.ToByte(Z) << 2) | (Convert.ToByte(O) << 6) | (Convert.ToByte(N) << 7));
                set
                {
                    C = (value & (1 << 1)) != 0;
                    Z = (value & (1 << 2)) != 0;
                    O = (value & (1 << 6)) != 0;
                    N = (value & (1 << 7)) != 0;
                }
            }
        };

        private readonly FLAGS Flags;

        // Random number generator;
        private readonly Random random;

        // Stateful values //

        private UInt32 Opcode;

        private byte InstructionCode;

        private byte Operand1;

        private byte Operand2;

        private byte Operand3;

        private UInt16 HHLL;

        private byte HH;

        private byte LL;

        private byte Z;

        private byte Y;

        private byte X;

        private byte N;

        // Maps opcodes to Instructions
        private readonly Dictionary<byte, Instruction> instructions;

        private readonly Memory memory;
        private readonly Graphics graphics;

        public CPU(Memory memory, Graphics graphics)
        {
            this.memory = memory;
            this.graphics = graphics;

            // The registers should be interpreted as signed (two's complements representation, unless otherwise stated).
            this.PC = 0x0000;
            this.SP = (UInt16)Memory.MemoryMap.Stack;
            this.R = new UInt16[16];
            this.Flags = new FLAGS();
            this.random = new Random();

            // Instruction Set Implementation

            this.instructions = JsonConvert.DeserializeObject<Dictionary<byte, Instruction>>(System.Text.Encoding.ASCII.GetString(Resources.Instructions));


            //================= 0x - Misc/Video/Audio =================//

            // NOP
            this[0x00] = delegate ()
            {

            };

            // CLS
            this[0x01] = delegate ()
            {
                graphics.Clear();
            };

            // VBLNK
            this[0x02] = delegate ()
            {
                graphics.VBLANK = true;
            };

            // BGC N
            this[0x03] = delegate ()
            {
                graphics.BG = N;
            };

            // SPR HHLL
            this[0x04] = delegate ()
            {
                graphics.SpriteW = LL;
                graphics.SpriteH = HH;
            };

            // DRW RX, RY, HHLL
            this[0x05] = delegate ()
            {
                Flags.C = graphics.DrawSprite(memory, HHLL, R[X], R[Y]);
            };

            // DRW RX, RY, RZ
            this[0x06] = delegate ()
            {
                Flags.C = graphics.DrawSprite(memory, R[Z], R[X], R[Y]);
            };

            // RND RX, HHLL
            this[0x07] = delegate ()
            {
                // Random number between [0, HHLL] Inclusively
                R[X] = (UInt16)random.Next(HHLL + 1);
            };

            // FLIP 0, 0 or FLIP 0, 1 or FLIP 1, 0 or FLIP 1, 1
            this[0x08] = delegate ()
            {
                switch (Operand3)
                {
                    case 0x00:
                        graphics.HFlip = false;
                        graphics.VFlip = false;
                        break;

                    case 0x01:
                        graphics.HFlip = false;
                        graphics.VFlip = true;
                        break;

                    case 0x02:
                        graphics.HFlip = true;
                        this.graphics.VFlip = false;
                        break;

                    case 0x03:
                        graphics.HFlip = true;
                        graphics.VFlip = true;
                        break;
                }
            };

            // SND0
            this[0x09] = delegate ()
            {
                // TODO
            };

            // SND1 HHLL
            this[0x0A] = delegate ()
            {
                // TODO
            };

            // SND2 HHLL
            this[0x0B] = delegate ()
            {
                // TODO
            };

            // SND3 HHLL
            this[0x0C] = delegate ()
            {
                // TODO
            };

            // SNP RX, HHLL
            this[0x0D] = delegate ()
            {
                // TODO
            };

            // SNG AD, VTSR
            this[0x0E] = delegate ()
            {
                // TODO
            };


            //================= 1x - Jumps (Branches) =================//

            // JMP HHLL
            this[0x10] = delegate ()
            {
                PC = HHLL;
            };

            // JMC HHLL
            this[0x11] = delegate ()
            {
                if (Flags.C)
                {
                    PC = HHLL;
                }
            };

            // Jx HHLL
            this[0x12] = delegate ()
            {
                // Branch if condition is true
                if (TestCondition(X))
                {
                    PC = HHLL;
                }
            };

            // JME RX, RY, HHLL
            this[0x13] = delegate ()
            {
                if (R[X] == R[Y])
                {
                    PC = HHLL;
                }
            };

            // CALL HHLL
            this[0x14] = delegate ()
            {
                memory.SetWord(SP, PC);
                SP += 2;
                PC = HHLL;
            };

            // RET
            this[0x15] = delegate ()
            {
                SP -= 2;
                PC = memory.ReadWord(SP);
            };

            // JMP RX
            this[0x16] = delegate ()
            {
                PC = R[X];
            };

            // Cx HHLL
            this[0x17] = delegate ()
            {
                if (TestCondition(X))
                    // Perform CALL
                    this[0x14]();
            };

            // CALL RX
            this[0x18] = delegate ()
            {
                memory.SetWord(SP, PC);
                SP += 2;
                PC = R[X];
            };


            //================= 2x - Loads =================//

            // LDI RX, HHLL
            this[0x20] = delegate ()
            {
                R[X] = HHLL;
            };

            // LDI SP, HHLL
            this[0x21] = delegate ()
            {
                SP = HHLL;
            };

            // LDM RX, HHLL
            this[0x22] = delegate ()
            {
                R[X] = memory.ReadWord(HHLL);
            };

            // LDM RX, RY
            this[0x23] = delegate ()
            {
                R[X] = memory.ReadWord(R[Y]);
            };

            // MOV RX, RY
            this[0x24] = delegate ()
            {
                R[X] = R[Y];
            };

            //================= 3x - Stores =================//

            // STM RX, HHLL
            this[0x30] = delegate ()
            {
                memory.SetWord(HHLL, R[X]);
            };

            // STM RX, RY
            this[0x31] = delegate ()
            {
                memory.SetWord(R[Y], R[X]);
            };

            //================= 4x - Addition =================//

            // ADDI RX, HHLL

            this[0x40] = delegate ()
            {
                Addition(ref R[X], R[X], HHLL);
            };

            // ADD RX, RY
            this[0x41] = delegate () 
            {
                Addition(ref R[X], R[X], R[Y]);
            };

            // ADD RX, RY, RZ
            this[0x42] = delegate ()
            {
                Addition(ref R[Z], R[X], R[Y]);
            };

            //================= 5x - Subtraction =================//

            // SUBI RX, HHLL
            this[0x50] = delegate ()
            {
                Subtraction(ref R[X], R[X], HHLL);
            };

            // SUB RX, RY
            this[0x51] = delegate ()
            {
                Subtraction(ref R[X], R[X], R[Y]);
            };

            // SUB RX, RY, RZ
            this[0x52] = delegate ()
            {
                Subtraction(ref R[Z], R[X], R[Y]);
            };

            // CMPI RX, HHLL
            this[0x53] = delegate ()
            {
                UInt16 discard = 0; ;
                Subtraction(ref discard, R[X], HHLL);
            };

            // CMP RX, RY
            this[0x54] = delegate ()
            {
                UInt16 discard = 0; ;
                Subtraction(ref discard, R[X], R[Y]);
            };

            //================= 6x - Bitwise AND (&) =================//

            // ANDI RX, HHLL
            this[0x60] = delegate ()
            {
                And(ref R[X], R[X], HHLL);
            };

            // AND RX, RY
            this[0x61] = delegate ()
            {
                And(ref R[X], R[X], R[Y]);
            };

            // AND RX, RY, RZ
            this[0x62] = delegate ()
            {
                And(ref R[Z], R[X], R[Y]);
            };

            // TSTI RX, HHLL
            this[0x63] = delegate ()
            {
                UInt16 discard = 0;
                And(ref discard, R[X], HHLL);
            };

            // TSTI RX, HHLL
            this[0x63] = delegate ()
            {
                UInt16 discard = 0;
                And(ref discard, R[X], R[Y]);
            };

            //================= 7x - Bitwise OR (|) =================//

            // ORI RX, HHLL
            this[0x70] = delegate ()
            {
                Or(ref R[X], R[X], HHLL);
            };

            // OR RX, RY
            this[0x71] = delegate ()
            {
                Or(ref R[X], R[X], R[Y]);
            };

            // OR RX, RY, RZ
            this[0x72] = delegate ()
            {
                Or(ref R[Z], R[X], R[Y]);
            };

            //================= 8x - Bitwise XOR (^) =================//

            // XORI RX, HHLL
            this[0x80] = delegate ()
            {
                Xor(ref R[X], R[X], HHLL);
            };

            // XOR RX, RY
            this[0x81] = delegate ()
            {
                Xor(ref R[X], R[X], R[Y]);
            };

            // XOR RX, RY, RZ
            this[0x82] = delegate ()
            {
                Xor(ref R[Z], R[X], R[Y]);
            };

            //================= 9x - Multiplication =================//

            // MULI RX, HHLL
            this[0x90] = delegate ()
            {
                Multiplication(ref R[X], R[X], HHLL);
            };

            // MUL RX, RY
            this[0x91] = delegate ()
            {
                Multiplication(ref R[X], R[X], R[Y]);
            };

            // MUL RX, RY, RZ
            this[0x92] = delegate ()
            {
                Multiplication(ref R[Z], R[X], R[Y]);
            };

            //================= Ax - Division =================//

            // DIVI RX, HHLL
            this[0xA0] = delegate ()
            {
                Division(ref R[X], R[X], HHLL);
            };

            // DIV RX, RY
            this[0xA1] = delegate ()
            {
                Division(ref R[X], R[X], R[Y]);
            };

            // DIV RX, RY, RZ
            this[0xA2] = delegate ()
            {
                Division(ref R[Z], R[X], R[Y]);
            };

            // MODI RX, HHLL or REMI RX, HHLL
            this[0xA3] = this[0xA6] = delegate ()
            {
                Mod(ref R[X], R[X], HHLL);
            };

            // MOD RX, RY or REM RX, RY
            this[0xA4] = this[0xA7] = delegate ()
            {
                Mod(ref R[X], R[X], R[Y]);
            };

            // MOD RX, RY, RZ or REM RX, RY, RZ
            this[0xA5] = this[0xA8] = delegate ()
            {
                Mod(ref R[Z], R[X], R[Y]);
            };

            //================= Bx - Logical/Arithmetic Shifts =================//

            // SHL RX, N
            this[0xB0] = delegate ()
            {
                LeftShift(ref R[X], R[X], N);
            };

            // SHR RX, N
            this[0xB1] = delegate ()
            {
                RightShift(ref R[X], R[X], N);
            };

            // SAR RX, N
            this[0xB2] = delegate ()
            {
                // Todo: Verify
                RightShiftSigned(ref R[X], R[X], N);
            };

            // SHL RX, RY
            this[0xB3] = delegate ()
            {
                LeftShift(ref R[X], R[X], R[Y]);
            };

            // SHR RX, RY
            this[0xB4] = delegate ()
            {
                RightShift(ref R[X], R[X], R[Y]);
            };

            // SAR RX, RY
            this[0xB5] = delegate ()
            {
                // Todo: Verify
                RightShiftSigned(ref R[X], R[X], R[Y]);
            };

            //================= Cx - Push/Pop =================//

            // PUSH RX
            this[0xC0] = delegate ()
            {
                memory.SetWord(SP, R[X]);
                SP += 2;
            };

            // POP RX
            this[0xC1] = delegate ()
            {
                SP -= 2;
                R[X] = memory.ReadWord(SP);
            };

            // PUSHALL
            this[0xC2] = delegate ()
            {
                for(UInt16 i = 0; i < 16; ++i)
                {
                    memory.SetWord(SP, R[i]);
                    SP += 2;
                }
            };

            // POPALL
            this[0xC3] = delegate ()
            {
                for (UInt16 i = 15; i >= 0; --i)
                {
                    SP -= 2;
                    R[i] = memory.ReadWord(SP);
                }
            };

            // PUSHF
            this[0xC4] = delegate ()
            {
                memory[SP] = Flags.Bits;
                SP += 2;
            };

            // POPF
            this[0xC5] = delegate ()
            {
                SP -= 2;
                Flags.Bits = memory[SP];
            };

            //================= Dx - Palette =================//

            // PAL HHLL
            this[0xD0] = delegate ()
            {
                UInt16 startAddress = HHLL;
                for(byte i = 0; i < 16; ++i)
                {
                    UInt16 address = (UInt16)(startAddress + (i * 3));
                    UInt32 color = (UInt32)((memory[(UInt16)(address + 0u)] << 16) | (memory[(UInt16)(address + 1u)] << 8) | (memory[(UInt16)(address + 2u)] << 0));
                    graphics.Palette[i] = color;
                }
            };

            // PAL RX
            this[0xD1] = delegate ()
            {
                UInt16 startAddress = R[X];
                for (byte i = 0; i < 16; ++i)
                {
                    UInt16 address = (UInt16)(startAddress + (i * 3));
                    UInt32 color = (UInt32)((memory[(UInt16)(address + 0u)] << 16) | (memory[(UInt16)(address + 1u)] << 8) | (memory[(UInt16)(address + 2u)] << 0));
                    graphics.Palette[i] = color;
                }
            };

            //================= Ex - Not/Neg =================//

            // NOTI RX, HHLL
            this[0xE0] = delegate ()
            {
                Not(ref R[X], HHLL);
            };

            // NOT RX
            this[0xE1] = delegate ()
            {
                Not(ref R[X], R[X]);
            };

            // NOT RX, RY
            this[0xE2] = delegate ()
            {
                Not(ref R[X], R[Y]);
            };

            // NEGI RX, HHLL
            this[0xE3] = delegate ()
            {
                Negate(ref R[X], HHLL);
            };

            // NEG RX
            this[0xE4] = delegate ()
            {
                Negate(ref R[X], R[X]);
            };

            // NEG RX, RY
            this[0xE5] = delegate ()
            {
                Negate(ref R[X], R[Y]);
            };
        }

        public void Execute()
        {
            Console.WriteLine($"{instructions[InstructionCode].Mnemonic} : {Opcode.ToString("x")}");
            Opcode = this.memory.ReadOpcode((UInt16)this.PC);
            InstructionCode = (byte)(Opcode >> 24);
            Operand1 = (byte)((Opcode & 0x00FF0000) >> 16);
            Operand2 = (byte)((Opcode & 0x0000FF00) >> 8);
            Operand3 = (byte)((Opcode & 0x000000FF) >> 0);
            HHLL = (UInt16)((Operand3 << 8) | (Operand2 << 0));
            HH = Operand3;
            LL = Operand2;
            Z = (byte)((Operand2 & 0x0F) >> 0);
            Y = (byte)((Operand1 & 0xF0) >> 4);
            X = (byte)((Operand1 & 0x0F) >> 0);
            N = (byte)((Operand2 & 0x0F) >> 0);
            this.PC += 4;
            // Execute Current instruction
            this[InstructionCode]();
            
        }

        private Action this[byte opcode]
        {
            get => this.instructions[opcode].Operation;
            set => this.instructions[opcode].Operation = value;
        }

        private void Addition(ref UInt16 store, UInt16 operand1, UInt16 operand2)
        {
            store = (UInt16)(operand1 + operand2);
            Flags.C = (UInt32)(operand1 + operand2) > UInt16.MaxValue;
            Flags.Z = store == 0x0000;
            Flags.O = (((Int16)operand1 < 0) == ((Int16)operand2 < 0)) && (((Int16)operand1 < 0) != ((Int16)store < 0));
            //Flags.O = ((Int16)operand1 >= 0 && (Int16)operand2 >= 0 && (Int16)store < 0) || 
            //    ((Int16)operand1 < 0 && (Int16)operand2 < 0 && (Int16)store >= 0); // Verify >= or >
            Flags.N = (Int16)(store) < 0;
        }

        private void Subtraction(ref UInt16 store, UInt16 operand1, UInt16 operand2)
        {
            store = (UInt16)(operand1 - operand2);
            Flags.C = (Int16)store < 0x0000;
            Flags.Z = store == 0x0000;
            Flags.O = ((Int16)store >= 0 && (Int16)operand1 < 0 && (Int16)operand2 >= 0) ||
                ((Int16)store < 0 && (Int16)operand1 >= 0 && (Int16)operand2 < 0);
            Flags.N = (Int16)(store) < 0;
        }

        private void Multiplication(ref UInt16 store, UInt16 operand1, UInt16 operand2)
        {
            store = (UInt16)(operand1 * operand2);
            Flags.C = (UInt32)(operand1 * operand2) > UInt16.MaxValue;
            Flags.Z = store == 0x0000;
            Flags.N = (Int16)(store) < 0;
        }

        private void Division(ref UInt16 store, UInt16 operand1, UInt16 operand2)
        {
            store = (UInt16)(operand1 / operand2);
            Flags.C = (Int16)operand1 % (Int16)operand2 != 0;
            Flags.Z = store == 0x0000;
            Flags.N = (Int16)(store) < 0;
        }

        private void Negate(ref UInt16 store, UInt16 operand1)
        {
            store = (UInt16)(-operand1);
            Flags.Z = store == 0x0000;
            Flags.N = (Int16)(store) < 0;
        }

        private void Mod(ref UInt16 store, UInt16 operand1, UInt16 operand2)
        {
            store = (UInt16)(operand1 % operand2);
            Flags.Z = store == 0x0000;
            Flags.N = (Int16)(store) < 0;
        }

        private void And(ref UInt16 store, UInt16 operand1, UInt16 operand2)
        {
            store = (UInt16)(operand1 & operand2);
            Flags.Z = store == 0;
            Flags.N = (Int16)(store) < 0;
        }

        private void Or(ref UInt16 store, UInt16 operand1, UInt16 operand2)
        {
            store = (UInt16)(operand1 | operand2);
            Flags.Z = store == 0;
            Flags.N = (Int16)(store) < 0;
        }

        private void Xor(ref UInt16 store, UInt16 operand1, UInt16 operand2)
        {
            store = (UInt16)(operand1 ^ operand2);
            Flags.Z = store == 0;
            Flags.N = (Int16)(store) < 0;
        }

        private void Not(ref UInt16 store, UInt16 operand1)
        {
            store = (UInt16)(~operand1);
            Flags.Z = store == 0;
            Flags.N = (Int16)(store) < 0;
        }

        private void LeftShift(ref UInt16 store, UInt16 operand1, UInt16 operand2)
        {
            store = (UInt16)(operand1 << operand2);
            Flags.Z = store == 0;
            Flags.N = (Int16)(store) < 0;
        }

        private void RightShift(ref UInt16 store, UInt16 operand1, UInt16 operand2)
        {
            store = (UInt16)(operand1 >> operand2);
            Flags.Z = store == 0;
            Flags.N = (Int16)(store) < 0;
        }

        private void RightShiftSigned(ref UInt16 store, UInt16 operand1, UInt16 operand2)
        {
            store = (UInt16)((Int16)operand1 >> (Int16)operand2);
            Flags.Z = store == 0;
            Flags.N = (Int16)(store) < 0;
        }

        private bool TestCondition(byte index)
        {
            switch (index)
            {
                // Equal (Zero)
                case 0x0:
                    return Flags.Z == true;

                // Not Equal (Non-Zero)
                case 0x1:
                    return Flags.Z = false;

                // Negative
                case 0x2:
                    return Flags.N == true;

                // Not-Negative (Positive or Zero)
                case 0x3:
                    return Flags.N == false;

                // Positive
                case 0x4:
                    return Flags.N == false && Flags.Z == false;

                // Overflow
                case 0x5:
                    return Flags.O == true;

                // No Overflow
                case 0x6:
                    return Flags.O == false;

                // Above (Unsigned Greater Than)
                case 0x7:
                    return Flags.C == false && Flags.Z == false;

                // Above Equal (Unsigned Greater Than or Equal)
                case 0x8:
                    return Flags.C == false;

                // Below (Unsigned Less Than)
                case 0x9:
                    return Flags.C == true;

                // Below Equal (Unsigned Less Than or Equal)
                case 0xA:
                    return Flags.C == true || Flags.Z == true;

                // Signed Greater Than
                case 0xB:
                    return Flags.O == Flags.N && Flags.Z == false;

                // Signed Greater Than or Equal
                case 0xC:
                    return Flags.O == Flags.N;

                // Signed Less Than
                case 0xD:
                    return Flags.O != Flags.N;

                // Signed Less Than or Equal
                case 0xE:
                    return Flags.O != Flags.N || Flags.Z == true;

                // Reserved for future use
                case 0xF:
                    return false;

                default:
                    return false;
            }
        }
    }
}
