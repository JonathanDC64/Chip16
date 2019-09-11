using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip16
{
    /// <summary>
    /// Chip16 has 64 KB (65,536 B) of memory.
    ///
    /// When reading word values from memory, they should be interpreted as signed, like registers, unless otherwise stated.
    /// There is no distinction between ROM and RAM; the contents of ROM are simply mapped into RAM, and may be overwritten.
    /// 
    /// Special addresses:
    /// 0x0000: Start of the ROM/RAM
    /// 0xFDF0: Start of the stack (512 B)
    /// 0xFFF0: Start of I/O ports(4 B)
    /// 
    /// The I/O ports are controller inputs.
    /// </summary>
    public class Memory
    {
        // Chip16 has 64 KB (65,536 B) of memory.
        private byte[] memory;

        public enum MemoryMap
        {
            ROM     = 0x0000,    // Start of the ROM/RAM
            Stack   = 0xFDF0,   // Start of the stack (512 B)
            IO      = 0xFFF0    // Start of I/O ports (4 B)
        }
        
        public Memory()
        {
            this.memory = new byte[65536]; // 64 KB
        }

        // Read 16-bit Word
        public UInt16 ReadWord(UInt16 address)
        {
            return (UInt16)(this.memory[address] << 8 | this.memory[address + 1]);
        }

        public UInt32 ReadOpcode(UInt16 address)
        {
            return (UInt32)((this.memory[address + 0] << 24) | (this.memory[address + 1] << 16) | (this.memory[address + 2] << 8) | (this.memory[address + 3] << 0));
        }

        public byte this[UInt16 address]
        {
            get { return this.memory[address]; }
            set { this.memory[address] = value; }
        }
    }
}
