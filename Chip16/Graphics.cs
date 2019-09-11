using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip16
{
    public class Graphics
    {
        private byte[,] graphics;

        // Color index of background layer
        private byte bg;
        public byte BG { get => this.bg; set => this.bg = value; }

        // Width of sprite(s) to draw
        private byte spritew;
        public byte SpriteW { get => this.spritew; set => this.spritew = value; }

        // Height of sprite(s) to draw
        private byte spriteh;
        public byte SpriteH { get => this.spriteh; set => this.spriteh = value; }

        // Flip sprite(s) to draw, horizontally
        private bool hflip = false;

        // Flip sprite(s) to draw, vertically
        private bool vflip = false;

        // The screen is updated at a frequency of 60 Hz. Every frame (~16.67ms), the internal VBLANK flag is raised, which can be waited on with the VBLNK instruction.
        public bool VBLANK
        {
            get; set;
        }

        public readonly static UInt32 WIDTH = 320;
        public readonly static UInt32 HEIGHT = 240;

        // Maps 4bit index to color value
        private Dictionary<byte, UInt32> palette = new Dictionary<byte, uint>()
        {
            {0x0, 0x000000}, // Black (transp. in FG)
            {0x1, 0x000000}, // Black
            {0x2, 0x888888}, // Gray
            {0x3, 0xBF3932}, // Red
            {0x4, 0xDE7AAE}, // Pink
            {0x5, 0x4C3D21}, // Dark brown
            {0x6, 0x905F25}, // Brown
            {0x7, 0xE49452}, // Orange
            {0x8, 0xEAD979}, // Yellow
            {0x9, 0x537A3B}, // Green
            {0xA, 0xABD54A}, // Light green
            {0xB, 0x252E38}, // Dark blue
            {0xC, 0x00467F}, // Blue
            {0xD, 0x68ABCC}, // Light blue
            {0xE, 0xBCDEE4}, // Sky blue
            {0xF, 0xFFFFFF}  // White
        };

        public Graphics()
        {
            // Chip16 uses a 320x240 screen resolution. The screen is updated at a frequency of 60 Hz. 
            // Every frame (~16.67ms), the internal VBLANK flag is raised, which can be waited on with the VBLNK instruction.
            this.graphics = new byte[240,320];
        }

        public void Clear()
        {
            this.bg = 0x00;
            for(int y = 0; y < HEIGHT; ++y)
            {
                for(int x = 0; x < WIDTH; ++x)
                {
                    this.graphics[y,x] = (byte)0x00;
                }
            }
        }

        public bool DrawSprite(Memory memory, UInt16 HHLL, int X, int Y)
        {
            UInt16 memPos = HHLL;
            int yStart = Y;
            int yEnd = Y + SpriteH;
            int yInc = 1;
            int xStart = X;
            int xEnd = X + SpriteW;
            int xInc = 2;

            for(int y = yStart; y < yEnd; y += yInc)
            {
                for(int x = xStart; x < xEnd; x += xInc)
                {
                    byte pixel = memory[memPos];
                    byte highPixel = (byte)(pixel >> 4);
                    byte lowPixel  = (byte)(pixel & 0x0F);
                    memPos += 1;
                }
            }
            return false;
        }

        public byte[,] RequestFrame()
        {
            this.VBLANK = true;
            return this.graphics;
        }
    }
}
