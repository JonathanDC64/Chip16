using System;
using System.Collections.Generic;

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
        public bool HFlip { get => this.hflip; set => this.hflip = value; }

        // Flip sprite(s) to draw, vertically
        private bool vflip = false;
        public bool VFlip { get => this.vflip; set => this.vflip = value; }

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
            {0x3, 0x3239BF}, // Red
            {0x4, 0xAE7ADE}, // Pink
            {0x5, 0x213D4C}, // Dark brown
            {0x6, 0x255F90}, // Brown
            {0x7, 0x5294E4}, // Orange
            {0x8, 0x79D9EA}, // Yellow
            {0x9, 0x3B7A53}, // Green
            {0xA, 0x4AD5AB}, // Light green
            {0xB, 0x382E25}, // Dark blue
            {0xC, 0x7F4600}, // Blue
            {0xD, 0xCCAB68}, // Light blue
            {0xE, 0xE4DEBC}, // Sky blue
            {0xF, 0xFFFFFF}  // White
        };

        public Dictionary<byte, UInt32> Palette
        {
            get => palette;
        }

        public Graphics()
        {
            // Chip16 uses a 320x240 screen resolution. The screen is updated at a frequency of 60 Hz. 
            // Every frame (~16.67ms), the internal VBLANK flag is raised, which can be waited on with the VBLNK instruction.
            this.graphics = new byte[HEIGHT,WIDTH];
            this.VBLANK = false;
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

        public void SetBG(byte bg)
        {
            this.bg = bg;
            for (int y = 0; y < HEIGHT; ++y)
            {
                for (int x = 0; x < WIDTH; ++x)
                {
                    if(this.graphics[y, x] == 0x00)
                    {
                        this.graphics[y, x] = this.bg;
                    }
                }
            }
        }

        public bool DrawSprite(Memory memory, UInt16 HHLL, Int32 X, Int32 Y)
        {
            UInt16 memPos = HHLL;
            Int32 yStart = Y;
            Int32 yEnd = (Int32)(Y + SpriteH);
            Int32 yInc = 1;
            Int32 xStart = X;
            Int32 xEnd = (Int32)(X + (SpriteW * 2));
            Int32 xInc = 2;
            Int32 hit = 0;

            for(Int32 y = yStart; y < yEnd; y += yInc)
            {
                for(Int32 x = xStart; x < xEnd; x += xInc)
                {
                    byte pixel = memory[memPos];
                    byte highPixel = (byte)(pixel >> 4); // x
                    byte lowPixel  = (byte)(pixel & 0x0F); // x + 1

                    Int32 drawX = HFlip ? xEnd - x - 2 : x;
                    Int32 drawY = VFlip ? yEnd - y - 1 : y;

                    if(drawX >= 0 && drawX < WIDTH && drawY >= 0 && drawY < HEIGHT)
                    {
                        hit += this[drawY, drawX + 0];
                        this[drawY, drawX + 0] = HFlip ? lowPixel : highPixel;
                        
                        if(drawX + 1 >= 0 && drawX + 1 < WIDTH)
                        {
                            hit += this[drawY, drawX + 1];
                            this[drawY, drawX + 1] = HFlip ? highPixel : lowPixel;
                        }
                    }

                    memPos += 1;
                }
            }
            return hit > 0;
        }

        public byte this[Int32 y, Int32 x]
        {
            get => this.graphics[y, x];
            set => this.graphics[y, x] = value;
        }

        public byte[,] RequestFrame()
        {
            this.VBLANK = true;
            return this.graphics;
        }
    }
}
