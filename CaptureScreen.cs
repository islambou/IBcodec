using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace xClient.Utils.ScreenCapture
{
    class CaptureScreen
    {

        


        static ImageConverter converter = new ImageConverter();
        private const int SRCCOPY = 0x00CC0020;
        public static Bitmap CaptureDesktop2()
        {
            Bitmap desktopBMP = new Bitmap(
					System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
					System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);

				Graphics g = Graphics.FromImage(desktopBMP);

				g.CopyFromScreen(0, 0, 0, 0,
				   new Size(
				   System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
				   System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height));
				g.Dispose();
                
                return desktopBMP;
        }

        public static Bitmap CaptureDesktop()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap screen = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);

            using (Graphics g = Graphics.FromImage(screen))
            {
                IntPtr destDeviceContext = g.GetHdc();
                IntPtr srcDeviceContext = NativeMethods.CreateDC("DISPLAY", null, null, IntPtr.Zero);

                NativeMethods.BitBlt(destDeviceContext, 0, 0, bounds.Width, bounds.Height, srcDeviceContext, bounds.X,
                    bounds.Y, SRCCOPY);

                NativeMethods.DeleteDC(srcDeviceContext);
                g.ReleaseHdc(destDeviceContext);
            }
  
            return screen;
        }
        
        public static Bitmap CaptureCursor(ref int x, ref int y)
        {
            Bitmap bmp;
            IntPtr hicon;
            Win32Stuff.CURSORINFO ci = new Win32Stuff.CURSORINFO();
            Win32Stuff.ICONINFO icInfo;
            ci.cbSize = Marshal.SizeOf(ci);
            if (Win32Stuff.GetCursorInfo(out ci))
            {
                if (ci.flags == Win32Stuff.CURSOR_SHOWING)
                {
                    hicon = Win32Stuff.CopyIcon(ci.hCursor);
                    if (Win32Stuff.GetIconInfo(hicon, out icInfo))
                    {
                        x = ci.ptScreenPos.x - ((int)icInfo.xHotspot);
                        y = ci.ptScreenPos.y - ((int)icInfo.yHotspot);

                        Icon ic = Icon.FromHandle(hicon);
                        bmp = ic.ToBitmap(); 
                        return bmp;
                    }
                }
            }

            return null;
        }

        public static Bitmap CaptureDesktopWithCursor()
        {
            int cursorX = 0;
            int cursorY = 0;
            Bitmap desktopBMP;
            Bitmap cursorBMP;
            Graphics g;
            Rectangle r;

            desktopBMP = CaptureDesktop();
            cursorBMP = CaptureCursor(ref cursorX, ref cursorY);
            if (desktopBMP != null)
            {
                if (cursorBMP != null)
                { 
                    r = new Rectangle(cursorX, cursorY, cursorBMP.Width, cursorBMP.Height);
                    g = Graphics.FromImage(desktopBMP);
                    g.DrawImage(cursorBMP, r);
                    g.Flush();

                    return desktopBMP;
                }
                else
                    return desktopBMP;
            }

            return null;

        }

            

  

 
        
        
    

        public static Stream Compress(Bitmap b ,long q)
        {
            EncoderParameter parameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, q);
            ImageCodecInfo _encoderInfo = GetEncoderInfo("image/jpeg");
            EncoderParameters _encoderParams = new EncoderParameters(2);
            _encoderParams.Param[0] = parameter;
            _encoderParams.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long)EncoderValue.CompressionRle);

            MemoryStream stream = new MemoryStream();

            b.Save(stream, _encoderInfo, _encoderParams);

            return stream;
             
        }

         private static  ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo[] imageEncoders = ImageCodecInfo.GetImageEncoders();
            int num2 = imageEncoders.Length - 1;
            for (int i = 0; i <= num2; i++)
            {
                if (imageEncoders[i].MimeType == mimeType)
                {
                    return imageEncoders[i];
                }
            }
            return null;
        }
    
    }
}
