using ImageMagick;
using Common.Services.ImageProcessing.Model;
using Common.Services.Static.Logger;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Common.Services.ImageProcessing.MetadataIO
{
    public static class OrientationHelper
    {
        public static void FixThumbnailOrientationIfNeed(string thumbnailPath, string originalFilePath)
        {
            try
            {
                if (!File.Exists(originalFilePath) || !File.Exists(thumbnailPath))
                {
                    return;
                }
                var orientation = MetadataExporter.GetOrientation(originalFilePath);
                if (orientation == null || orientation.Id == 1)
                {
                    return;
                }
                Logger.Info($"[{Path.GetFileName(originalFilePath)}] Need to fix orientation for thumbnail file", orientation, new { originalFilePath, thumbnailPath });
                using (var fileStream = new MemoryStream())
                {
                    using (var readStream = File.OpenRead(thumbnailPath))
                    {
                        readStream.Seek(0, SeekOrigin.Begin);
                        readStream.CopyTo(fileStream);
                        fileStream.Seek(0, SeekOrigin.Begin);
                    }
                    File.Delete(thumbnailPath);
                    using (var img = new MagickImage(fileStream))
                    {
                        switch (orientation.MirrorType)
                        {
                            case OrientationMirrorType.FLIP:
                                img.Flip();
                                break;
                            case OrientationMirrorType.FLOP:
                                img.Flop();
                                break;
                        }
                        img.Orientation = OrientationType.Undefined;
                        if (orientation.RotateDegrees > 0)
                        {
                            img.Rotate(orientation.RotateDegrees);
                        }
                        img.Write(thumbnailPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "FixThumbnailOrientationIfNeed exception", thumbnailPath, originalFilePath);
            }
        }
        public static void RotateFileToCorrectOrientation(string filePath, int degree = 0)
        {
            var fileStream = new MemoryStream();
            using (var readStream = File.OpenRead(filePath))
            {
                readStream.Seek(0, SeekOrigin.Begin);
                readStream.CopyTo(fileStream);
                fileStream.Seek(0, SeekOrigin.Begin);
            }
            File.Delete(filePath);
            using (var img = new MagickImage(fileStream))
            {
                img.Rotate(degree);
                img.Write(filePath);
            }
        }
        public static int GetFileOrientationDegree(string originalPath)
        {
            try
            {
                var metadata = MetadataExporter.GetMetadata(originalPath);
                if (!metadata.Keys.Any(t => t.ToUpper() == "ORIENTATION"))
                {
                    return 0;
                }
                var orientationData = metadata.First(t => t.Key.ToUpper() == "ORIENTATION");
                int.TryParse(Regex.Match(orientationData.Value, @"\d+").Value, out var numberData);
                return numberData;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
