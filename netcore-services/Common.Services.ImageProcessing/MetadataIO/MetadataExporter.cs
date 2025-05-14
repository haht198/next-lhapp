using Common.Services.ImageProcessing.Model;
using Common.Services.Static;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Common.Services.ImageProcessing.MetadataIO
{
    public class OrientationCacheObject
    {
        public OrientationElement Orientation { get; set; }
        public FileInfo OriginalFileInfo { get; set; }
    }
    public static class MetadataExporter
    {
        private static bool isRunningExifProcess = false;
        private static object lockObject = new object();
        private static ConcurrentDictionary<string, OrientationCacheObject> orientationCached = new ConcurrentDictionary<string, OrientationCacheObject>();

        public static Dictionary<string, string> GetMetadata(string originalFile)
        {
            if (string.IsNullOrEmpty(UserSetting.ExifTool) || string.IsNullOrEmpty(UserSetting.MetadataRootFolderPath))
            {
                return new Dictionary<string, string>();
            }
            var metadataFile = GetMetadataFilePath(originalFile);
            if (!File.Exists(metadataFile))
            {
                DoExportMetadata(FileTypeHelper.IsEIPFormat(originalFile) ? ImageInformationHelper.GetRawFileFromEIPFile(originalFile) : originalFile, metadataFile);
            }
            return ParserMetadata(metadataFile);
        }

        public static Dictionary<string, string> GetMetadataByKeys(string filePath, IList<string> metadataKeys)
        {
            var result = new Dictionary<string, string>();
            var arguments = BuildGetMetadataCommandArgumentsMultiKeys(FileTypeHelper.IsEIPFormat(filePath) ? ImageInformationHelper.GetRawFileFromEIPFile(filePath) : filePath, null, metadataKeys);
            var (cmdResult, _) = Utils.RunProcess(arguments);
            if (string.IsNullOrEmpty(cmdResult))
            {
                return result;
            }
            using (StringReader reader = new StringReader(cmdResult))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var data = line.Split(':');

                    if (data.Count() >= 2)
                    {
                        var property = data[0].Trim().Replace(" ", "").ToLower();
                        var value = data[1].Trim();

                        if (!result.ContainsKey(property))
                            result.Add(property, value);
                    }
                }
            }
            return result;
        }

        public static string GetMetadataFilePath(string originalPath)
        {
            if (!Directory.Exists(UserSetting.MetadataRootFolderPath))
            {
                Directory.CreateDirectory(UserSetting.MetadataRootFolderPath);
            }
            var folderPath = Utils.Md5Hash(Uri.EscapeDataString(Path.GetDirectoryName(originalPath).Replace("/", "").Replace("\\", "")));
            var fileHashed = Utils.Md5Hash(Uri.EscapeDataString(Path.GetFileName(originalPath)));
            return Path.Combine(UserSetting.MetadataRootFolderPath, folderPath.ToString().ToUpper(), $".{fileHashed.ToString().ToUpper()}.xmp.kelvin");
        }

        public static OrientationElement GetOrientation(string originalFile)
        {
            if (string.IsNullOrEmpty(UserSetting.ExifTool))
            {
                return null;
            }
            if (!File.Exists(originalFile))
            {
                return null;
            }
            if (orientationCached.Any(o => o.Key == originalFile))
            {
                var cacheObject = orientationCached.First(o => o.Key == originalFile);
                var originalFileInfo = new FileInfo(originalFile);
                if (cacheObject.Value.OriginalFileInfo.LastWriteTimeUtc >= originalFileInfo.LastWriteTimeUtc)
                {
                    return cacheObject.Value.Orientation;
                }
                else
                {
                    orientationCached.TryRemove(originalFile, out var _);
                }
            }
            OrientationElement orientationResult = null;
            if (FileTypeHelper.IsCommonFormat(Path.GetExtension(originalFile)) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                orientationResult = DoGetOrientationBySystemDrawing(originalFile);
            }
            else
            {
                orientationResult = DoGetOrientationByExifTool(FileTypeHelper.IsEIPFormat(originalFile) ? ImageInformationHelper.GetRawFileFromEIPFile(originalFile) : originalFile);
            }
            if (orientationResult != null)
            {
                orientationCached.TryAdd(originalFile, new OrientationCacheObject()
                {
                    Orientation = orientationResult,
                    OriginalFileInfo = new FileInfo(originalFile)
                });
            }
            return orientationResult;
        }

        private static OrientationElement DoGetOrientationBySystemDrawing(string originalFile, int retryCount = 0)
        {
            using (var bm = new Bitmap(originalFile))
            {
                int OrientationId = 0x0112;
                int orientation_index = Array.IndexOf(bm.PropertyIdList, OrientationId);
                if (orientation_index < 0) return null;
                return OrientationName.GetById(bm.GetPropertyItem(OrientationId).Value[0]);
            }
        }

        private static OrientationElement DoGetOrientationByExifTool(string originalFile, int retryCount = 0)
        {
            if (!File.Exists(UserSetting.ExifTool))
            {
                return null;
            }
            if (retryCount >= 3)
            {
                return null;
            }
            try
            {

                if (isRunningExifProcess)
                {
                    Task.Delay(500).GetAwaiter().GetResult();
                    return DoGetOrientationByExifTool(originalFile, retryCount++);
                }
                lock (lockObject)
                {
                    isRunningExifProcess = true;
                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = UserSetting.ExifTool,
                            Arguments = $"-orientation -n \"{originalFile}\"",
                            RedirectStandardOutput = true
                        };
                        process.Start();
                        process.WaitForExit();
                        isRunningExifProcess = false;
                        var output = process.StandardOutput.ReadToEnd();
                        if (int.TryParse(Regex.Match(output, @"\d+").Value, out var orientationId))
                        {
                            return OrientationName.GetById(orientationId);
                        }
                        return null;
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        private static Dictionary<string, string> ParserMetadata(string metadataFile)
        {
            if (!File.Exists(metadataFile))
            {
                return new Dictionary<string, string>();
            }
            var rawText = File.ReadAllText(metadataFile);
            var newLinesRegex = new Regex(@"\r\n|\n|\r", RegexOptions.Singleline);
            var rawData = newLinesRegex.Split(rawText).Select(t => t.Trim()).ToList();
            var result = new Dictionary<string, string>();
            foreach (var data in rawData)
            {
                if (string.IsNullOrEmpty(data))
                {
                    continue;
                }
                var pairData = data.Split(':');
                if (result.Keys.Any(t => t == pairData[0].Trim()))
                {
                    result[pairData[0].Trim()] = pairData.Length > 1 ? pairData[1].Trim() : "";
                }
                else
                {
                    result.Add(pairData[0].Trim(), pairData.Length > 1 ? pairData[1].Trim() : "");
                }
            }
            return result;
        }

        private static bool DoExportMetadata(string originalFile, string metadataFile, int retryCount = 0)
        {
            if (!File.Exists(UserSetting.ExifTool))
            {
                return false;
            }
            if (retryCount >= 3)
            {
                return true;
            }
            try
            {

                if (isRunningExifProcess)
                {
                    Task.Delay(500).GetAwaiter().GetResult();
                    return DoExportMetadata(originalFile, metadataFile, retryCount++);
                }
                lock (lockObject)
                {
                    isRunningExifProcess = true;
                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = UserSetting.ExifTool,
                            Arguments = $" \"{originalFile}\" -w \"%c{metadataFile}\"",
                            RedirectStandardOutput = true
                        };
                        process.Start();
                        process.WaitForExit();
                        isRunningExifProcess = false;
                    }
                }
            }
            catch (Exception) { }
            return true;
        }

        private static string BuildGetMetadataCommandArgumentsMultiKeys(string filePath, string tempConfigPath, IList<string> metadataKeys)
        {
            if (tempConfigPath == null)
            {
                tempConfigPath = Path.Combine(UserSetting.WorkspaceFolder, "metadata", "_temporary", $"exiftool-{Guid.NewGuid()}.config");
            }
            var arguments = new List<string>();
            arguments.Add($"-config {tempConfigPath.KeepSafeArgumentValue()}");
            foreach (var item in metadataKeys)
            {
                arguments.Add($"-{item.KeepSafeArgumentKey()}");
            }

            arguments.Add(filePath.KeepSafeArgumentValue());
            return string.Join(" ", arguments);
        }
    }
}
