using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;

namespace Common.Services.Static
{
    public static class Utils
    {
        private static readonly Random Random = new Random();
        private static readonly DateTime MinJsTime = new(1970, 1, 1, 0, 0, 0);
        private static HttpClient _httpClientCheckConnection;
        private static readonly string UrlCheckConnection = "https://download.creativeforce.io/ping";
        public static bool HasInternetConnection = true;
        public static string Md5Hash(string value)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(value);
                var hashBytes = md5.ComputeHash(inputBytes);
                var sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString().ToUpper();
            }
        }

        public static string CalculateContentMd5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static long CalculateFileLength(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return 0;
            }
            return new FileInfo(fileName).Length;
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        public static long GetTime(DateTime date)
        {
            return (long)(date - MinJsTime).TotalMilliseconds;
        }

        public static DateTime GetDateFromUnixTime(long time)
        {
            return MinJsTime.AddMilliseconds(time);
        }

        public static bool PortIsAvailable(string host, int port)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect(host, port);
                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }
        public static IEnumerable<string> UnzipFile(string zipFilePath, string extractFolder)
        {
            if (Directory.Exists(extractFolder))
            {
                Directory.Delete(extractFolder, true);
            }
            ZipFile.ExtractToDirectory(zipFilePath, extractFolder!);
            return Directory.GetFiles(extractFolder);
        }

        public static void ZipFolderAsEipFile(string folder, string zipfile)
        {
            using (var outFileStream = new FileStream(zipfile, FileMode.Create))
            using (var zipArchive = new ZipArchive(outFileStream, ZipArchiveMode.Create))
            {
                var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories); //.OrderByDescending(i => i);
                foreach (var file in files)
                {
                    var relativePath = file.Replace("\\", "/").Replace(folder.Replace("\\", "/") + "/", "");

                    var zipEntry = zipArchive.CreateEntry(relativePath, CompressionLevel.NoCompression);
                    using (var zipStream = zipEntry.Open())
                    using (var inFileStream = new FileStream(file, FileMode.Open))
                    {
                        inFileStream.CopyTo(zipStream);
                    }
                }
            }
        }

        public static (string standardOutput, string standardError) RunProcess(string arguments, bool redirectStandardError = false)
        {
            var psi = new ProcessStartInfo
            {
                FileName = UserSetting.ExifTool,
                Arguments = arguments,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = redirectStandardError,
                StandardOutputEncoding = Encoding.UTF8
            };
            var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit();

                var standardError = string.Empty;
                if (redirectStandardError)
                {
                    standardError = process.StandardError.ReadToEnd();
                }

                var standardOutput = process.StandardOutput.ReadToEnd();
                return (standardOutput, standardError);
            }

            return (null, null);
        }

        public static string GetEipFileHelperFolder(string eipFilePath)
        {
            return Path.Combine(UserSetting.WorkspaceFolder, "metadata", "eip-helpers", Utils.Md5Hash(Uri.EscapeDataString(Path.GetFileName(eipFilePath))));
        }

        public static string GetVideoInfoFilePath(string videoFilePath)
        {
            var folderHashed = Md5Hash(Uri.EscapeDataString(Path.GetDirectoryName(videoFilePath)!.Replace("/", "").Replace("\\", "")));
            var fileHashed = Md5Hash(Uri.EscapeDataString(Path.GetFileName(videoFilePath)));
            if (!Directory.Exists(Path.Combine(UserSetting.WorkspaceFolder, "metadata", folderHashed))) {
                Directory.CreateDirectory(Path.Combine(UserSetting.WorkspaceFolder, "metadata", folderHashed));
            }
            return Path.Combine(UserSetting.WorkspaceFolder, "metadata", folderHashed, $"{fileHashed}.videoinfo.kelvin");
        }

        public static bool AlwaysValidCertificateForRequest (
            object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true; // **** Always accept
        }
    }
}
