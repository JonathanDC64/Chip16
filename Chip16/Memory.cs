using System;

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

        public UInt16 LoadRom(byte[] data)
        {
            // 16 byte header

            // Magic Number = CH16 = 0x43483136, 0x43 = C, 0x48 = H, 0x31 = 1, 0x36 = 6
            UInt32 magicNumber = (UInt32)((data[0x00] << 24) | (data[0x01] << 16) | (data[0x02] << 8) | (data[0x03] << 0));

            // Unused reserved byte
            byte reserved = data[0x04];

            // Version of rom spec
            byte specificationVersion = data[0x05];

            // Excludes header
            UInt32 romSize = (UInt32)((data[0x06] << 24) | (data[0x07] << 16) | (data[0x08] << 8) | (data[0x09] << 0));

            // Initial value of PC
            UInt16 startAddress = (UInt16)((data[0x0A] << 8) | (data[0x0B]) << 0);

            // ROM checksum
            UInt32 CRC32 = (UInt32)((data[0x0C] << 24) | (data[0x0D] << 16) | (data[0x0E] << 8) | (data[0x0F] << 0));

            // Load ROM into memory

            // First byte of ROM starts at 0x10, only ofset if rom has header
            byte romOffset = (byte)(magicNumber == 0x43483136 ? 0x10 : 0x00);

            for(UInt32 address = 0; address < memory.Length; ++address)
            {
                if(address + romOffset < data.Length)
                {
                    memory[address] = data[address + romOffset];
                }
                else
                {
                    memory[address] = 0x00;
                }
            }

            return startAddress;
        }
    }
}
