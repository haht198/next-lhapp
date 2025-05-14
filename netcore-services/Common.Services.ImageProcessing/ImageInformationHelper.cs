using ImageMagick;
using Common.Services.ImageProcessing.Model;
using Common.Services.Static;
using Common.Services.Static.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using LibVLCSharp.Shared;

namespace Common.Services.ImageProcessing
{
    public class ImageFileContentCache
    {
        public string FilePath { get; set; }
        public DateTime CacheDate { get; set; }
        public byte[] FileContent { get; set; }
        public MemoryStream ToReadStream()
        {
            return new MemoryStream(FileContent);
        }
    }
    public class VideoInformation
    {
        public string internet_media_type { get; set; }
        public string codec { get; set; }
        public float frame_rate { get; set; }
        public int frame_count { get; set; }
        public float duration { get; set; }
        public float height { get; set; }
        public float width { get; set; }
    }
    public class FFProbeData_Stream
    {
        public string codec_name { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string avg_frame_rate { get; set; }
        public float duration { get; set; }
        public string nb_frames { get; set; }
    }
    public class FFProbeData
    {
        public List<FFProbeData_Stream> streams { get; set; }
    }
    public static class ImageInformationHelper
    {
        private static object _locker_fileContentCached = new object();
        private static List<ImageFileContentCache> _fileContentCached = new List<ImageFileContentCache>();
        public static ImageDimention GetImageDimention(string filePath)
        {
            try
            {
                if (!FileTypeHelper.IsSupported(Path.GetExtension(filePath)))
                {
                    return null;
                }
                using (var fileStream = File.OpenRead(filePath))
                using (var image = new MagickImage(fileStream))
                {
                    return new ImageDimention
                    {
                        Height = image.Height,
                        Width = image.Width
                    };
                }
            }
            catch (Exception)
            {
                return new ImageDimention
                {
                    Height = 0,
                    Width = 0
                };
            }
        }

        public static bool IsImageCorrupted(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return false;
                }
                if (!File.Exists(filePath))
                {
                    return false;
                }
                if (!FileTypeHelper.IsImageFormats(filePath))
                {
                    return false;
                }
                var fileExtension = Path.GetExtension(filePath);
                if (!FileTypeHelper.IsSupported(fileExtension))
                {
                    return true;
                }
                var fileType = FileTypeHelper.String2FileType(fileExtension);
                if ((int)fileType >= 1000)
                {
                    return false;
                }
                var result = false;
                var rootFileStream = new MemoryStream();
                using (var readStream = File.OpenRead(filePath))
                {
                    readStream.Seek(0, SeekOrigin.Begin);
                    readStream.CopyTo(rootFileStream);
                    rootFileStream.Seek(0, SeekOrigin.Begin);
                }
                ReadImage(rootFileStream, fileExtension);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Check file is corrupted exception: {ex.Message}");
                return false;
            }

        }

        /// <summary>
        /// Reference document: https://en.wikipedia.org/wiki/List_of_file_signatures
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool IsValid(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return false;
                }
                if (!File.Exists(filePath))
                {
                    return false;
                }
                var fileExtension = Path.GetExtension(filePath);
                if (!FileTypeHelper.IsSupported(fileExtension))
                {
                    return false;
                }
                switch (FileTypeHelper.String2FileType(fileExtension))
                {
                    case SupportedFileTypeEnum.JPG:
                    case SupportedFileTypeEnum.JPEG:
                        return FileTypeHelper.IsJPG(filePath);
                    case SupportedFileTypeEnum.PNG:
                        return FileTypeHelper.IsPNG(filePath);
                    case SupportedFileTypeEnum.TIF:
                    case SupportedFileTypeEnum.TIFF:
                    case SupportedFileTypeEnum.NEF:
                    case SupportedFileTypeEnum.NRW:
                    case SupportedFileTypeEnum.ARW:
                        return FileTypeHelper.IsTIF(filePath);
                    case SupportedFileTypeEnum.PSD:
                        return FileTypeHelper.IsPSD(filePath);
                    case SupportedFileTypeEnum.CR2:
                        return FileTypeHelper.IsCR2(filePath);
                    case SupportedFileTypeEnum.CR3:
                        return FileTypeHelper.IsCR3(filePath);
                    case SupportedFileTypeEnum.CRW:
                        return FileTypeHelper.IsCRW(filePath);
                    case SupportedFileTypeEnum.PDF:
                        return FileTypeHelper.IsPDF(filePath);
                    case SupportedFileTypeEnum.MP4:
                        return FileTypeHelper.IsMP4(filePath);
                    case SupportedFileTypeEnum.MOV:
                        return true;
                    default: return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Check file is vaid exception: {ex.Message}");
                return true;
            }
        }
        public static Stream GetFileContentStreamWithCache(string filePath, bool forceNoCache = false)
        {
            MemoryStream fileContent = null;
            try
            {
                lock (_locker_fileContentCached)
                {
                    if (!forceNoCache && _fileContentCached.Any(f => f.FilePath == filePath))
                    {
                        var cacheData = _fileContentCached.First(f => f.FilePath == filePath);
                        if (cacheData.CacheDate.AddMinutes(5) < DateTime.UtcNow)
                        {
                            _fileContentCached.Remove(cacheData);
                        }
                        else
                        {
                            fileContent = _fileContentCached.First(f => f.FilePath == filePath).ToReadStream();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error when try to get file content from cache");
                fileContent = null;
            }
            if (fileContent == null)
            {
                fileContent = new MemoryStream();
                using (var readStream = File.OpenRead(filePath))
                {
                    readStream.Seek(0, SeekOrigin.Begin);
                    readStream.CopyTo(fileContent);
                    fileContent.Seek(0, SeekOrigin.Begin);
                }
                try
                {
                    lock (_locker_fileContentCached)
                    {
                        if (_fileContentCached.Count >= 200)
                        {
                            _fileContentCached.RemoveAt(0);
                        }
                        if (_fileContentCached.Any(f => f.FilePath == filePath))
                        {
                            _fileContentCached.RemoveAt(_fileContentCached.FindIndex(vf => vf.FilePath == filePath));
                        }
                        _fileContentCached.Add(new ImageFileContentCache
                        {
                            FilePath = filePath,
                            CacheDate = DateTime.UtcNow,
                            FileContent = fileContent.ToArray()
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error when try to set file content to cache");
                }
                fileContent.Seek(0, SeekOrigin.Begin);
            }
            return fileContent;
        }

        public static string GetRawFileFromEIPFile(string eipFilePath)
        {
            var startTime = DateTime.Now;
            var eipHeplerFolder = Utils.GetEipFileHelperFolder(eipFilePath);
            if (Directory.Exists(eipHeplerFolder))
            {
                return Directory.GetFiles(eipHeplerFolder).FirstOrDefault(f => FileTypeHelper.IsRawFormat(Path.GetExtension(f)));
            }
            else
            {
                var files = Utils.UnzipFile(eipFilePath, Utils.GetEipFileHelperFolder(eipFilePath));

                Logger.Tracing(eipFilePath).Info($"[{Path.GetFileName(eipFilePath)}] Unzipfile took {(DateTime.Now - startTime).TotalMilliseconds}ms");
                return files.FirstOrDefault(f => FileTypeHelper.IsRawFormat(Path.GetExtension(f)));
            }
        }

        public static VideoInformation GetVideoInformation(string filePath)
        {
            var informationFile = Utils.GetVideoInfoFilePath(filePath);
            if (File.Exists(informationFile))
            {
                try
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<VideoInformation>(File.ReadAllText(informationFile));
                    return data;
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, $"GetVideoInformation from cached file exception: {ex.Message}");
                }
            }

            var ffprobeExecuteFile = Path.Combine(ProgramArguments.ServicesFolder,"ffmpeg", RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffprobe.exe" : "ffprobe");
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = ffprobeExecuteFile,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        RedirectStandardError = false,
                        Arguments = $"-v error -of default=noprint_wrappers=0 -print_format json -show_streams -show_format \"{filePath}\"",
                    };
                    process.Start();
                    process.WaitForExit(3000);
                    var standardOutput = process.StandardOutput.ReadToEnd();
                    var ffprobeData = Newtonsoft.Json.JsonConvert.DeserializeObject<FFProbeData>(standardOutput);

                    if (!string.IsNullOrEmpty(standardOutput) && ffprobeData != null && ffprobeData.streams != null && ffprobeData.streams.Count > 0)
                    {
                        var streamData = ffprobeData.streams[0];
                        var result = new VideoInformation
                        {
                            codec = streamData.codec_name,
                            duration = streamData.duration * 1000,
                            frame_count = int.Parse(new Regex("([0-9]+)").Matches(streamData.nb_frames).First().Value),
                            frame_rate = float.Parse(new Regex("([0-9]+)").Matches(streamData.avg_frame_rate).First().Value),
                            height = streamData.height,
                            width = streamData.width,
                        };
                        File.WriteAllText(informationFile, Newtonsoft.Json.JsonConvert.SerializeObject(result));
                        return result;
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, $"GetVideoInformation exception: {ex.Message}");
                return null;
            }
            
        }


        public static VideoInformation GetVideoInformationV2(string filePath)
        {
            var informationFile = Utils.GetVideoInfoFilePath(filePath);
            if (File.Exists(informationFile))
            {
                try
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<VideoInformation>(File.ReadAllText(informationFile));
                    return data;
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, $"GetVideoInformation from cached file exception: {ex.Message}");
                }
            }
            try {
                var _libVLC = new LibVLC();
                using var media = new Media(_libVLC, new Uri(filePath));
                _ = media.Parse(MediaParseOptions.ParseLocal).GetAwaiter().GetResult();
                var durationInMilisecond = media.Duration;
                var durationInsecond = media.Duration / 1000.0;
                var codec = 0;
                var width = 0;
                var height = 0;
                var frame_rate = 0;
                if (media.Tracks != null && media.Tracks.Length > 0 && media.Tracks[0].TrackType == TrackType.Video)
                {
                    frame_rate = (int)media.Tracks[0].Data.Video.FrameRateNum;
                    width = (int)media.Tracks[0].Data.Video.Width;
                    height = (int)media.Tracks[0].Data.Video.Height;
                    codec = (int)media.Tracks[0].Codec;
                }
                //var meta = media.Meta();
                var result = new VideoInformation
                {
                    duration = media.Duration,
                    codec = codec.ToString(),
                    width = width,
                    height = height,
                    frame_rate = frame_rate,
                    frame_count = (int)(durationInsecond * frame_rate)
                };
                return result;
            } 
            catch (Exception ex)
            {
                Logger.Warning(ex, $"GetVideoInformation exception: {ex.Message}");
                return null;
            }
        }
        private static void ReadImage(Stream inputStream, string fileType)
        {
            var isCorruptedLog = false;
            var image = new MagickImage();
            switch (fileType.Trim().ToUpper())
            {
                case ".PSD":
                case ".TIF":
                case ".TIFF":
                    var layerCollection = new MagickImageCollection(inputStream);
                    if (layerCollection.Count > 1)
                    {
                        foreach (var layer in layerCollection)
                        {
                            layer.Alpha(AlphaOption.Remove);
                        }
                        var flattenImg = layerCollection.Flatten();
                    }
                    else
                    {
                        image.Warning += (sender, e) =>
                        {
                            isCorruptedLog = e.Message.ToUpper().StartsWith("CORRUPT");
                        };
                        image.Read(inputStream);
                    }
                    break;
                default:
                    inputStream.Seek(0, SeekOrigin.Begin);
                    image.Warning += (sender, e) =>
                    {
                        isCorruptedLog = e.Message.ToUpper().StartsWith("CORRUPT");
                    };
                    image.Read(inputStream);
                    break;
            }
            if (isCorruptedLog)
            {
                throw new Exception("File has corrupted");
            }
        }

        private static void ReadVideoInformation(string filePath)
        {

        }
    }
}
