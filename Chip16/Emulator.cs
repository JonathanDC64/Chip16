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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
        private readonly Sound sound;

        public Emulator()
        {
            this.memory = new Memory();
            this.graphics = new Graphics();
            this.input = new Input();
            this.sound = new Sound();
            this.cpu = new CPU(this.memory, this.graphics);
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

        private UInt32 ARGBtoABGR(UInt32 argb)
        {
            return ((argb & 0xFF000000) << 0) | ((argb & 0x000000FF) << 16) | ((argb & 0x0000FF00) << 0) | ((argb & 0x00FF0000) >> 16);
        }

        public UInt32[] PixelData
        {
            get
            {
                UInt32[] pixels = new UInt32[Graphics.WIDTH * Graphics.HEIGHT];
                UInt32 BGColor = ARGBtoABGR((UInt32)((this.graphics.Palette[this.graphics.BG])));

                for (Int32 y = 0; y < Graphics.HEIGHT; ++y)
                {
                    for (Int32 x = 0; x < Graphics.WIDTH; ++x)
                    {
                        byte pixel = this.graphics[y, x];
                        UInt32 color = ARGBtoABGR(this.graphics.Palette[pixel]);
                        pixels[y * Graphics.WIDTH + x] = pixel > 0 ? color : BGColor;
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
