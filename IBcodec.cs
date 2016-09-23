using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

/*
Made BY : Bouderbala Islam
Contact Info :
 email : Bouderbalaislam [at] Gmail.com
 skype : kanare007
 facebook : fb.com/islambdrbl

    The MIT License (MIT)
    Copyright (c) 2016 AnguisCaptor
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

namespace IBCodec
{
    class IBcodec
    {
        Bitmap Old;  // the image we are going to use as a refrence
        byte[] _Old; // byte array contains data of "Bitmap Old"
        Bitmap New;  // the seconde image , which will be compared to the first one
 
        public List<Rectangle> Blocks; // list of rectangles to be transmeted 
        public List<Stream> BlocksData;// list of images as streams to be transmeted
        public int quality;            // compression paremeter

        public void Code(Bitmap N, int q)
        {
     
            BitmapData NewData = null;
            BitmapData OldData = null;
            const int nbpp = 4; // number of bytes per pixel
            New = N;
            int width = New.Width;
            int height = New.Height;
            int left = width;
            int right = 0;
            int top = height;
            int bottom = 0;

            List<Rectangle> horizantals = new List<Rectangle>(); 
            Blocks = new List<Rectangle>();
            BlocksData = new List<Stream>();

            int lastX = -1, lastY = -1;
            if (Old == null || quality != q)
            {
                //if ther is no old image to compare to , just send the new one

                Old = New;
                quality = q;
                BlocksData.Add(CaptureScreen.Compress(New, q));
                Blocks.Add(new Rectangle(0, 0, width, height));
                return;
            }

            else
            {
                try
                {

                    NewData = New.LockBits(
                                        new Rectangle(0, 0, New.Width, New.Height),
                                        ImageLockMode.ReadOnly, New.PixelFormat);
                    OldData = Old.LockBits(
                        new Rectangle(0, 0, Old.Width, Old.Height),
                        ImageLockMode.ReadOnly, Old.PixelFormat);


                    int strideNew = NewData.Stride / nbpp;
                    int strideOld = OldData.Stride / nbpp;


                    IntPtr scanNew0 = NewData.Scan0;
                    IntPtr scanOld0 = OldData.Scan0;
                    _Old = new byte[Old.Width * Old.Height * nbpp];
                    unsafe
                    {
                        int* pNew = (int*)(void*)scanNew0;
                        int* pPrev = (int*)(void*)scanOld0;
                        byte* pScanNew0 = (byte*)scanNew0.ToInt32();


                        fixed (byte* ptr = _Old)
                        {

                            NativeMethods.memcpy(new IntPtr(ptr), scanOld0, (uint)(Old.Width * Old.Height * nbpp));


                            // we are going to cutt the image in horizontal pieces each time we finde something has changed
                            for (int y = 0; y < New.Height; ++y)
                            {
                                int offset = (y * Old.Width * nbpp);
                                if (NativeMethods.memcmp(ptr + offset, pScanNew0 + offset, (uint)(Old.Width * nbpp)) != 0)
                                {
                                    if (y < top) { top = y; }
                                    if (y > bottom) { bottom = y; }
                                    lastY = y;
                                }


                                if ((y - lastY > 0 && lastY != -1) || (lastY == New.Height - 1 && top == 0))
                                {
                                    horizantals.Add(new Rectangle(0, top, Old.Width, bottom - top + 1));
                                    top = height;
                                    bottom = 0;
                                    lastY = -1;

                                }

                            }

                            // now we are going to cutt each horizantal piece vertically each time we finde something has changed 
                            for (int i = 0; i < horizantals.Count; i++)
                            {
                                left = horizantals[i].X + horizantals[i].Width;
                                right = horizantals[i].X;
                                for (int x = horizantals[i].X; x < horizantals[i].X + horizantals[i].Width; x++)
                                {
                                    pNew = (int*)(void*)scanNew0 + (horizantals[i].Y * strideNew);
                                    pPrev = (int*)(void*)scanOld0 + (horizantals[i].Y * strideOld);
                                    for (int y = horizantals[i].Y; y < horizantals[i].Y + horizantals[i].Height; ++y)
                                    {
                                        if ((pNew + x)[0] != (pPrev + x)[0])
                                        {
                                            if (x <= left) { left = x; }
                                            if (x >= right) { right = x; }
                                            lastX = x;
                                        }

                                        pNew += strideNew;
                                        pPrev += strideOld;
                                    }

                                    if ((x - lastX > 0 && lastX != -1) || (left == horizantals[i].X && right + 1 == horizantals[i].X + horizantals[i].Width + 1))
                                    {
                                        Blocks.Add(new Rectangle(left, horizantals[i].Top, right - left, horizantals[i].Top + horizantals[i].Height));
                                        left = horizantals[i].X + horizantals[i].Width;
                                        right = horizantals[i].X;
                                        lastX = -1;



                                    }

                                }
                            }


                        }


                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("err : " + e.Message);
                }
                finally
                {
                    if (NewData != null)
                    {
                        New.UnlockBits(NewData);
                    }
                    if (OldData != null)
                    {
                        Old.UnlockBits(OldData);
                    }
                }
                // now we have a list of rectangles where changes has happened , so we are going to creat small images from each rectangle
                for (int i = 0; i < Blocks.Count; i++)
                {
                    try
                    {
                        Rectangle bounds = Blocks[i];
                        bounds.Width = bounds.Width == 0 ? 1 : bounds.Width = bounds.Width;
                        bounds.Height = bounds.Width == 0 ? 1 : bounds.Height = bounds.Height;

                        Bitmap diff = new Bitmap(bounds.Width, bounds.Height);
                        Graphics _graphics = Graphics.FromImage(diff);

                        _graphics.DrawImage(New, 0, 0, bounds, GraphicsUnit.Pixel);

                        BlocksData.Add(CaptureScreen.Compress(diff, q));
                        _graphics.Flush();
                        _graphics.Dispose();
                        diff.Dispose();


                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("adding exception : " + e.Message);
                    }
                  

                }
                Old = New; // now the seconde image becomes the refrence 
            }

        }






    }
}
