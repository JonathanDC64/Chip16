using System;
using System.IO;

namespace Chip16
{
    // Facade for VM components, Interfaces with Front-End
    public class Emulator
    {
        private readonly Memory memory;
        private readonly CPU cpu;
        private readonly Input input;
        private readonly Graphics graphics;
        private readonly Sound sound;

        public Emulator()
        {
            this.memory = new Memory();
            this.graphics = new Graphics();
            this.input = new Input();
            this.sound = new Sound();
            this.cpu = new CPU(this.memory, this.graphics, this.input, this.sound);
        }

        public void LoadRom(string filePath)
        {
            byte[] romData = File.ReadAllBytes(filePath);
            this.cpu.PC = this.memory.LoadRom(romData);
        }

        public void ExecuteCPU()
        {
            this.cpu.Execute();
        }

        public UInt32[] PixelData
        {
            get
            {
                UInt32[] pixels = new UInt32[Graphics.WIDTH * Graphics.HEIGHT];

                for (Int32 y = 0; y < Graphics.HEIGHT; ++y)
                {
                    for (Int32 x = 0; x < Graphics.WIDTH; ++x)
                    {
                        byte pixel = this.graphics[y, x];
                        if (pixel > 0x00)
                        {
                            UInt32 color = this.graphics.Palette[pixel];
                            pixels[y * Graphics.WIDTH + x] = (UInt32)((color) | (0xFF << 24));
                        }
                        else // If pixel index == 0, draw transparent
                        {
                            pixels[y * Graphics.WIDTH + x] = (UInt32)0x00000000; // alpha is 0
                        }
                    }
                }
                return pixels;
            }
        }

        public bool VBlank
        {
            get => graphics.VBLANK;
            set => graphics.VBLANK = value;
        }

        public void SetKeyState(bool[] input)
        {
            this.input.SetState(input);

            this.memory[(UInt16)Memory.MemoryMap.IO] = this.input.State;
        }
    }
}
