using Common.Services.Static;
using Common.Services.Static.Logger;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LibVLCSharp.Shared;

namespace Common.Services.ImageProcessing.MakeThumbnail.Tools
{
    public class VLCTool : IMakeThumbnailTool
    {
        private LibVLC _libVLC = null;

        public VLCTool() {}
        public MemoryStream Process(string originalFilePath, int maxSize, int quality, SupportedFileTypeEnum fileType, bool nocache = false)
        {
            var thumbnail = ExtractThumbnailFromVideoFile(originalFilePath, maxSize);
            if (!string.IsNullOrEmpty(thumbnail) && new FileInfo(thumbnail).Length > 0)
            {
                var thumbnailStream = new MemoryStream();
                using (var previewFileStream = File.OpenRead(thumbnail))
                {
                    previewFileStream.CopyTo(thumbnailStream);
                }
                File.Delete(thumbnail);
                return thumbnailStream;
            }
            return null;
        }

        private string ExtractThumbnailFromVideoFile(string originalPath, int maxSize)
        {
            var tempFolder = Path.Combine(UserSetting.WorkspaceFolder, "thumbnails", "_temporary");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
            var previewFilePath = Path.Combine(tempFolder, $"{Guid.NewGuid()}.JPG");
            try
            {
                _libVLC = new LibVLC("--vout=dummy");
                using (var media = new Media(_libVLC, new Uri(originalPath)))
                {
                    using (var mp = new MediaPlayer(media))
                    {
                        mp.Play();
                        mp.Mute = true;
                        //while(!mp.IsPlaying)
                        //{
                        //    System.Threading.Thread.Sleep(100);  // check media player start
                        //}
                        //mp.Position = 0.25f;
                        System.Threading.Thread.Sleep(100); // waiting mediaplayer play vide
                        long duration = media.Duration;
                        mp.SeekTo(TimeSpan.FromMilliseconds(duration * 0.25));
                        System.Threading.Thread.Sleep(500);
                        bool success = mp.TakeSnapshot(0, previewFilePath, 0, 0);
                        if (success)
                        {
                            //System.Threading.Thread.Sleep(1000); // take and save snapshoot
                            mp.Stop();
                            return previewFilePath;
                        } else
                        {
                            mp.Stop();
                            return string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, $"Generate thumnail for video exception: {ex.Message}");
            }
            return string.Empty;
        }

    }
}
