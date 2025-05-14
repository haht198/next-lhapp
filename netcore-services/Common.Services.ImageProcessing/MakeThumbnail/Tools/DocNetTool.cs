//using System;
//using System.IO;
//using Docnet.Core.Models;
//using Docnet.Core;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;

//namespace Common.Services.ImageProcessing.MakeThumbnail.Tools
//{
//    public class DocNetTool : IMakeThumbnailTool
//    {
//        public DocNetTool()
//        {
//        }

//        public MemoryStream Process(string originalFilePath, int maxSize, int quality, SupportedFileTypeEnum fileType)
//        {
//            using (var docReader = DocLib.Instance.GetDocReader(originalFilePath, new PageDimensions(4000, 4000)))
//            using (var pageReader = docReader.GetPageReader(0))
//            {
//                var rawBytes = pageReader.GetImage();
//                var width = pageReader.GetPageWidth();
//                var height = pageReader.GetPageHeight();

//                var characters = pageReader.GetCharacters();

//                using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
//                {
//                    AddBytes(bmp, rawBytes);
//                    var result = ReplaceTransparency(bmp, Color.White);
//                    //DrawRectangles(bmp, characters);
//                    var stream = new MemoryStream();
//                    result.Save(stream, ImageFormat.Png);
//                    return stream;
//                }
//            }
//        }

//        public System.Drawing.Bitmap ReplaceTransparency(System.Drawing.Bitmap bitmap, System.Drawing.Color background)
//        {
//            /* Important: you have to set the PixelFormat to remove the alpha channel.
//             * Otherwise you'll still have a transparent image - just without transparent areas */
//            var result = new System.Drawing.Bitmap(bitmap.Size.Width, bitmap.Size.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
//            var g = System.Drawing.Graphics.FromImage(result);

//            g.Clear(background);
//            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
//            g.DrawImage(bitmap, 0, 0);

//            return result;
//        }

//        private void AddBytes(Bitmap bmp, byte[] rawBytes)
//        {
//            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
//            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
//            var pNative = bmpData.Scan0;

//            Marshal.Copy(rawBytes, 0, pNative, rawBytes.Length);
//            bmp.UnlockBits(bmpData);
//        }

//        private void DrawRectangles(Bitmap bmp, IEnumerable<Character> characters)
//        {
//            var pen = new Pen(Color.Red);

//            using (var graphics = Graphics.FromImage(bmp))
//                foreach (var c in characters)
//                {
//                    var rect = new Rectangle(c.Box.Left, c.Box.Top, c.Box.Right - c.Box.Left, c.Box.Bottom - c.Box.Top);
//                    graphics.DrawRectangle(pen, rect);
//                }
//        }
//    }
//}
