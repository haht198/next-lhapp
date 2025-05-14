using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Common.Services.Static;
using ImageMagick;
using Common.Services.ImageProcessing.MetadataIO;
using Common.Services.Static.Logger;
using System.Linq;

namespace Common.Services.ImageProcessing.MakeThumbnail.Tools
{
    public class ExifTool : IMakeThumbnailTool
    {
        private string _defaultImage = "";
        private string DefaultImage
        {
            get
            {
                if (string.IsNullOrEmpty(_defaultImage))
                {
                    _defaultImage = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "default-image.JPEG");
                }
                return _defaultImage;
            }
        }
        public MemoryStream Process(string originalFilePath, int maxSize, int quality, SupportedFileTypeEnum fileType, bool nocache = false)
        {
            // if (FileTypeHelper.IsEIPFormat(originalFilePath))
            // {
            //     return DoProcess(ImageInformationHelper.GetRawFileFromEIPFile(originalFilePath), maxSize, quality, 0);
            // }
            // else
            {
                return DoProcess(originalFilePath, maxSize, quality, 0);
            }
        }

        private MemoryStream DoProcess(string originalFilePath, int maxSize, int quality, int runningCount)
        {
            if (runningCount >= 3)
            {
                return null;
                //var thumbnailStream = new MemoryStream();
                //using (var defaultImage = File.OpenRead(DefaultImage))
                //{
                //    defaultImage.CopyTo(thumbnailStream);
                //}
                //return thumbnailStream;
            }
            var tempFullViewImage = ExtractThumbnailImageFromRawFile(originalFilePath);
            if (!string.IsNullOrEmpty(tempFullViewImage) && new FileInfo(tempFullViewImage).Length > 0)
            {
                OrientationHelper.FixThumbnailOrientationIfNeed(tempFullViewImage, originalFilePath);

                var thumbnailStream = new MemoryStream();
                using (var previewFileStream = File.OpenRead(tempFullViewImage))
                using (var image = new MagickImage(tempFullViewImage))
                {
                    image.Orientation = OrientationType.Undefined;
                    image.Write(thumbnailStream);
                }
                File.Delete(tempFullViewImage);
                return thumbnailStream;
            }

            Task.Delay(500).GetAwaiter().GetResult();
            return DoProcess(originalFilePath, maxSize, quality, ++runningCount);
        }

        private string ExtractThumbnailImageFromRawFile(string originalPath)
        {
            var fullResThumbnail = ExtractPreviewViewImageFromRawFile(originalPath);
            if (!string.IsNullOrEmpty(fullResThumbnail) && new FileInfo(fullResThumbnail).Length > 0)
            {
                return fullResThumbnail;
            }
            var extractedThumbnail = ExtractJpgFromRawFile(originalPath);
            if (!string.IsNullOrEmpty(extractedThumbnail) && new FileInfo(extractedThumbnail).Length > 0)
            {
                return extractedThumbnail;
            }
            return null;
        }

        private string ExtractJpgFromRawFile(string originalPath)
        {
            var exifPath = UserSetting.ExifTool;
            var tempPreviewFilePrefix = $".preview.kelvin.";
            var previewFile = $"{tempPreviewFilePrefix}{Path.GetFileNameWithoutExtension(originalPath)}.JPG";
            var exiftPreviewFile = $"%d{tempPreviewFilePrefix}%f.JPG";
            var previewFilePath = Path.Combine(Path.GetDirectoryName(originalPath), previewFile);
            if (File.Exists(previewFilePath))
            {
                File.Delete(previewFilePath);
            }
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = exifPath,
                        Arguments = $"-b -jpgfromraw -w \"{exiftPreviewFile}\" \"{originalPath}\"",
                    };
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception) { }

            if (File.Exists(previewFilePath))
            {
                return previewFilePath;
            }
            return string.Empty;
        }

        private string ExtractPreviewViewImageFromRawFile(string originalPath)
        {
            var exifPath = UserSetting.ExifTool;
            var tempPreviewFilePrefix = $".preview.kelvin.";
            var previewFile = $"{tempPreviewFilePrefix}{Path.GetFileNameWithoutExtension(originalPath)}.JPG";
            var exiftPreviewFile = $"%d{tempPreviewFilePrefix}%f.JPG";
            var previewFilePath = Path.Combine(Path.GetDirectoryName(originalPath), previewFile);
            if (File.Exists(previewFilePath))
            {
                File.Delete(previewFilePath);
            }
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = exifPath,
                        Arguments = $"-b -PreviewImage -w {exiftPreviewFile} -ext {Path.GetExtension(originalPath).Replace(".", "").ToString().ToLower()} -r \"{originalPath}\"",
                    };
                    process.Start();
                    process.WaitForExit();

                    if (File.Exists(previewFilePath))
                    {
                        return previewFilePath;
                    }
                }
            }
            catch (Exception) { }

            if (File.Exists(previewFilePath))
            {
                return previewFilePath;
            }
            return string.Empty;
        }
    }
}
