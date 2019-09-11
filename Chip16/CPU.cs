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

            public HashSet<Flags> Flags;

            public Action Operation { get; set; }

            private Dictionary<string, Flags> flagMap = new Dictionary<string, Flags>()
            {
                ["C"] = CPU.Flags.C,
                ["Z"] = CPU.Flags.Z,
                ["O"] = CPU.Flags.O,
                ["N"] = CPU.Flags.N
            };

            [JsonConstructor]
            public Instruction(string Mnemonic, byte Opcode, List<string> Flags)
            {
                this.Mnemonic = Mnemonic;
                this.Opcode = Opcode;
                this.Flags = new HashSet<Flags>();
                foreach (string flag in Flags)
                {
                    this.Flags.Add(flagMap[flag]);
                }
            }

            public void Execute()
            {
                this.Operation();
            }
        }

        // 16 bit program counter
        private UInt16 PC;

        // 16 bit stack pointer
        private UInt16 SP;

        // 16x 16 bit general purpose registers (R0..RF)
        private UInt16[] R;

        // 8 bit flag register
        private object FLAGS = new
        {
            C = false,
            Z = false,
            O = false,
            N = false
        };

        public enum Flags
        {
            C = 0x1, // Carry
            Z = 0x2, // Zero
            O = 0x6, // Overflow,
            N = 0x7, // Negative
        }

        // Maps opcodes to Instructions
        private Dictionary<byte, Instruction> instructions;

        private Memory memory;
        private Graphics graphics;

        public CPU(Memory memory, Graphics graphics)
        {
            this.memory = memory;
            this.graphics = graphics;

            // The registers should be interpreted as signed (two's complements representation, unless otherwise stated).
            this.PC = 0x0000;
            this.SP = (UInt16)Memory.MemoryMap.Stack;
            this.R = new UInt16[16];

            // Instruction Set Implementation

            this.instructions = JsonConvert.DeserializeObject<Dictionary<byte, Instruction>>(System.Text.Encoding.ASCII.GetString(Resources.Instructions));

            // NOP
            this[0x00] = delegate ()
            {

            };

            // CLS
            this[0x01] = delegate ()
            {
                this.graphics.Clear();
            };

            // VBLNK
            this[0x02] = delegate ()
            {
                // TODO : Implement VBLANK
            };

            // BGC N
            this[0x03] = delegate ()
            {
                this.graphics.BG = this.Operand2;
            };

            // SPR HHLL
            this[0x04] = delegate ()
            {
                this.graphics.SpriteW = LL;
                this.graphics.SpriteH = HH;
            };

            // DRW RX, RY, HHLL
            this[0x05] = delegate ()
            {
                
            };
        }

        private Action this[byte opcode]
        {
            get => this.instructions[opcode].Operation;
            set => this.instructions[opcode].Operation = value;
        }

        private void Execute()
        {
            UInt32 opcode = this.memory.ReadOpcode((UInt16)this.PC);

            byte instructionCode = (byte)(opcode >> 24);

            byte operand1 = (byte)((opcode & 0x00FF0000) >> 16);
            byte operand2 = (byte)((opcode & 0x0000FF00) >>  8);
            byte operand3 = (byte)((opcode & 0x000000FF) >>  0);
        }

        private UInt32 Opcode
        {
            get => this.memory.ReadOpcode((UInt16)this.PC);
        }

        private byte InstructionCode
        {
            get => (byte)(Opcode >> 24);
        }

        private byte Operand1
        {
            get => (byte)((Opcode & 0x00FF0000) >> 16);
        }

        private byte Operand2
        {
            get => (byte)((Opcode & 0x0000FF00) >> 8);
        }

        private byte Operand3
        {
            get => (byte)((Opcode & 0x000000FF) >> 0);
        }

        private UInt16 HHLL
        {
            get => (UInt16)((Operand3 << 8) | (Operand2 << 0));
        }

        private byte HH
        {
            get => Operand3;
        }

        private byte LL
        {
            get => Operand2;
        }

        private byte Z
        {
            get => (byte)((Operand2 & 0x0F) >> 0);
        }

        private byte Y
        {
            get => (byte)((Operand1 & 0xF0) >> 4);
        }

        private byte X
        {
            get => (byte)((Operand1 & 0x0F) >> 0);
        }

        private byte[] Operands
        {
            get
            {
                UInt32 opcode = this.memory.ReadOpcode((UInt16)this.PC);
                byte operand1 = (byte)((opcode & 0x00FF0000) >> 16);
                byte operand2 = (byte)((opcode & 0x0000FF00) >> 8);
                byte operand3 = (byte)((opcode & 0x000000FF) >> 0);
                return new byte[] { operand1, operand2, operand3 };
            }
        }
    }
}
