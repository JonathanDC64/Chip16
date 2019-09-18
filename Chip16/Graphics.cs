using System;
using System.Collections.Generic;

namespace Chip16
{
    public class Graphics
    {
        private readonly byte[,] graphics;

        // Color index of background layer
        public byte BG { get; set; }

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
        private readonly UInt32[] palette = new UInt32[]
        {
            0x00_00_00_00, // 0x0 Black (transp. in FG)
            0xFF_00_00_00, // 0x1 Black
            0xFF_88_88_88, // 0x2 Gray
            0xFF_BF_39_32, // 0x3 Red
            0xFF_DE_7A_AE, // 0x4 Pink
            0xFF_4C_3D_21, // 0x5 Dark brown
            0xFF_90_5F_25, // 0x6 Brown
            0xFF_E4_94_52, // 0x7 Orange
            0xFF_EA_D9_79, // 0x8 Yellow
            0xFF_53_7A_3B, // 0x9 Green
            0xFF_AB_D5_4A, // 0xA Light green
            0xFF_25_2E_38, // 0xB Dark blue
            0xFF_00_46_7F, // 0xC Blue
            0xFF_68_AB_CC, // 0xD Light blue
            0xFF_BC_DE_E4, // 0xE Sky blue
            0xFF_FF_FF_FF  // 0xF White
        };

        public UInt32[] Palette
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
            this.BG = 0x00;
            for(int y = 0; y < HEIGHT; ++y)
            {
                for(int x = 0; x < WIDTH; ++x)
                {
                    this.graphics[y,x] = (byte)0x00;
                }
            }
        }

        public bool DrawSprite(Memory memory, UInt16 HHLL, Int32 X, Int32 Y)
        {
            //int iy, iy_st, iy_end, iy_inc;
            //int ix, ix_st, ix_end, ix_inc;
            //int i, j, hit;
            //
            ///* If nothing will be on-screen, may as well exit. */
            //if (X > 319 || Y > 239 || SpriteW == 0 || SpriteH == 0 || Y + SpriteH < 0 || X + SpriteW * 2 < 0)
            //    return false;
            //hit = 0;
            ///* Sort out what direction the sprite will be drawn in. */
            //ix_st = 0;
            //ix_end = SpriteW * 2;
            //ix_inc = 2;
            //if (HFlip)
            //{
            //    ix_st = SpriteW * 2 - 2;
            //    ix_end = -2;
            //    ix_inc = -2;
            //}
            //iy_st = 0;
            //iy_end = SpriteH;
            //iy_inc = 1;
            //if (VFlip)
            //{
            //    iy_st = SpriteH - 1;
            //    iy_end = -1;
            //    iy_inc = -1;
            //}
            ///* Start drawing... */
            //for (iy = iy_st, j = 0; iy != iy_end; iy += iy_inc, ++j)
            //{
            //    for (ix = ix_st, i = 0; ix != ix_end; ix += ix_inc, i += 2)
            //    {
            //        byte p, hp, lp;
            //        /* Bounds checking for memory accesses. */
            //        if (i + X < 0 || i + X > 318 || j + Y < 0 || j + Y > 239)
            //            continue;
            //        p = memory[(UInt16)(HHLL + SpriteW * iy + ix / 2)];
            //        hp = (byte)(p >> 4);
            //        lp = (byte)(p & 0x0f);
            //        /* Flip the pixel couple if necessary. */
            //        if (HFlip)
            //        {
            //            int t = lp;
            //            lp = hp;
            //            hp = (byte)t;
            //        }
            //        /* Draw the pixels if not transparent. */
            //        if (hp != 0)
            //        {
            //            hit += graphics[Y + j, X + i];
            //            graphics[Y + j, X + i] = hp;
            //        }
            //        if (lp != 0)
            //        {
            //            hit += graphics[Y + j, X + i + 1];
            //            graphics[Y + j, X + i + 1] = lp;
            //        }
            //    }
            //}
            //return (hit > 0);

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
            
                    byte leftPixel = HFlip ? lowPixel : highPixel;
                    byte rightPixel = HFlip ? highPixel : lowPixel;
            
                    Int32 drawX = HFlip ? xEnd - x - 2 : x;
                    Int32 drawY = VFlip ? yEnd - y - 1 : y;
            
                    if (drawX >= 0 && drawX < WIDTH && drawY >= 0 && drawY < HEIGHT)
                    {
                        // Draw existing pixel if leftPixel is transparent
                        if(leftPixel != 0x00)
                        {
                            hit += leftPixel != 0 ? this[drawY, drawX + 0] : 0;
                            this[drawY, drawX + 0] = leftPixel;
                        }
                        
            
                        if (drawX + 1 >= 0 && drawX + 1 < WIDTH)
                        {
                            // Draw existing pixel if rightPixel is transparent
                            if(rightPixel != 0x00)
                            {
                                hit += rightPixel != 0 ? this[drawY, drawX + 1] : 0;
                                this[drawY, drawX + 1] = rightPixel;
                            }
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
