using ImageMagick;
using Common.Services.Static.Logger;
using System;
using System.IO;

namespace Common.Services.ImageProcessing.MakeThumbnail.Tools
{
    internal class MagickNetTool : IMakeThumbnailTool
    {
        private readonly MagickFormat thumbnailFormat;
        private readonly Interlace thumbnailInterlay;
        private readonly MagickColor thumbnailBackgroundColor;
        private readonly int needToCompressImageFromSize = 1000;

        public MagickNetTool()
        {
            thumbnailFormat = MagickFormat.Jpeg;
            thumbnailInterlay = Interlace.Jpeg;
            thumbnailBackgroundColor = MagickColors.White;
        }
        public MemoryStream Process(string originalFile, int maxSize, int quality, SupportedFileTypeEnum fileType, bool nocache = false)
        {
            //var rootFileStream = ImageInformationHelper.GetFileContentStreamWithCache(originalFile, nocache);
            var rootFilePath = originalFile;
            using (var rootFileStream = File.OpenRead(rootFilePath))
            using (var image = GetMagickImage(rootFileStream, originalFile))
            {
                var output = new MemoryStream();
                if (CanUseOriginalImageForThumbnail(image, maxSize))
                {
                    rootFileStream.Seek(0, SeekOrigin.Begin);
                    rootFileStream.CopyTo(output);
                }
                else
                {
                    image.BackgroundColor = thumbnailBackgroundColor;
                    image.Format = thumbnailFormat;
                    image.Interlace = thumbnailInterlay;
                    image.Quality = quality;
                    if (fileType == SupportedFileTypeEnum.TIF || fileType == SupportedFileTypeEnum.TIFF)
                    {
                        image.Alpha(AlphaOption.Off);
                    }
                    else
                    {
                        image.Alpha(AlphaOption.Remove);
                    }
                    if (maxSize >= Math.Max(image.Width, image.Height))
                    {
                        maxSize = Math.Max(image.Width, image.Height);
                    }
                    int width, height;
                    if (image.Width > image.Height)
                    {
                        width = maxSize;
                        height = Convert.ToInt32(image.Height * maxSize / (double)image.Width);
                    }
                    else
                    {
                        width = Convert.ToInt32(image.Width * maxSize / (double)image.Height);
                        height = maxSize;
                    }
                    image.Thumbnail(width, height);
                    //image.Orientation = OrientationType.Undefined;
                    // if (fileType == SupportedFileTypeEnum.ARW)
                    // {
                    //     // Manual adjustment of brightness and contrast
                    //     image.BrightnessContrast(new Percentage(20), new Percentage(10));
                    //     // // Giảm màu vàng (màu xanh +20%, màu đỏ -20%)
                    //     // image.Modulate(new Percentage(100), new Percentage(80), new Percentage(120));
                    //     //
                    //     // // Áp dụng bộ lọc màu để giảm hiệu ứng ngả màu xanh
                    //     // image.Evaluate(Channels.Green, EvaluateOperator.Divide, 1.1);
                    // }
                    image.AutoOrient();
                    image.Write(output);
                    if (Math.Max(width, height) > needToCompressImageFromSize)
                    {
                        try
                        {
                            output.Seek(0, SeekOrigin.Begin);
                            var optimizer = new ImageOptimizer();
                            optimizer.Compress(output);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning(ex, "Exception when compress image", originalFile);
                        }
                    }
                }
                return output;
            }
        }

        private MagickImage GetMagickImage(Stream inputStream, string originalFilePath)
        {
            //if (fileType == SupportedFileTypeEnum.PSD)
            //{
            //    var layerCollection = new MagickImageCollection(inputStream);
            //    if (layerCollection.Count > 1)
            //    {
            //        var temp = layerCollection.Flatten();
            //        //foreach (var layer in layerCollection)
            //        //{
            //        //    layer.Alpha(AlphaOption.Remove);
            //        //}
            //        temp.Alpha(AlphaOption.Remove);
            //        return new MagickImage(temp);
            //    }
            //    else
            //    {
            //        inputStream.Seek(0, SeekOrigin.Begin);
            //        return new MagickImage(inputStream);
            //    }
            //}
            inputStream.Seek(0, SeekOrigin.Begin);

            if (FileTypeHelper.IsNEFFormat(originalFilePath))
            {
                return new MagickImage(inputStream, new MagickReadSettings() { Format = MagickFormat.Nef })
                {
                    BackgroundColor = MagickColors.White
                };
            }else if(FileTypeHelper.IsNRWFormat(originalFilePath))
            {
                return new MagickImage(inputStream, new MagickReadSettings() { Format = MagickFormat.Nrw })
                {
                    BackgroundColor = MagickColors.White
                };
            }
            // else if (FileTypeHelper.IsARWFormat(originalFilePath))
            // {
            //     // If the file is ARW, read it as ARW format
            //     var image = new MagickImage(inputStream, new MagickReadSettings { Format = MagickFormat.Arw });
            //
            //     // Set the background color to white
            //     image.BackgroundColor = MagickColors.Black;
            //
            //     return image;
            // }

            return new MagickImage(inputStream)
            {
                BackgroundColor = MagickColors.White
            };
        }
        private bool CanUseOriginalImageForThumbnail(IMagickImage image, int maxSize)
        {
            if (!CanUseDirectlyOriginalImage(image))
            {
                return false;
            }
            return Math.Max(image.Width, image.Height) <= maxSize;
        }
        private bool CanUseDirectlyOriginalImage(IMagickImage image)
        {
            return image.Format == MagickFormat.Jpe ||
                image.Format == MagickFormat.Jpg ||
                image.Format == MagickFormat.Jpeg;
        }
    }
}
