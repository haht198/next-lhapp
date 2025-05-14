using Common.Services.ImageProcessing.MakeThumbnail.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Common.Services.ImageProcessing.MakeThumbnail
{
    public interface IMakeThumbnailTool
    {
        MemoryStream Process(string originalFilePath, int maxSize, int quality, SupportedFileTypeEnum fileType, bool nocache = false);
    }
    internal class MakeThumbnailToolAbility
    {
        public Type TypeOfTool { get; set; }
        public List<SupportedFileTypeEnum> CanProcessFiles { get; set; }
        public List<OSPlatform> SupportedPlatform { get; set; }
    }
    public static class MakeThumbnailToolFactory
    {
        private static List<MakeThumbnailToolAbility> _tools = new List<MakeThumbnailToolAbility>()
        {
            new MakeThumbnailToolAbility
            {
                TypeOfTool = typeof(MagickNetTool),
                CanProcessFiles = new List<SupportedFileTypeEnum>
                {
                    SupportedFileTypeEnum.JPG,
                    SupportedFileTypeEnum.JPEG,
                    SupportedFileTypeEnum.PNG,
                    SupportedFileTypeEnum.TIF,
                    SupportedFileTypeEnum.TIFF,
                    SupportedFileTypeEnum.PSD,
                    // SupportedFileTypeEnum.CR2,
                    SupportedFileTypeEnum.CR3,
                    SupportedFileTypeEnum.CRW,
                    SupportedFileTypeEnum.NEF,
                    SupportedFileTypeEnum.NRW,
                    // SupportedFileTypeEnum.ARW,
                    SupportedFileTypeEnum.EIP,
                },
                SupportedPlatform = new List<OSPlatform>
                {
                    OSPlatform.Windows,
                    OSPlatform.OSX,
                    OSPlatform.Linux
                }
            },
            new MakeThumbnailToolAbility
            {
                TypeOfTool = typeof(ExifTool),
                CanProcessFiles = new List<SupportedFileTypeEnum>
                {
                    SupportedFileTypeEnum.ARW,
                    SupportedFileTypeEnum.CR2
                },
                SupportedPlatform = new List<OSPlatform>
                {
                    OSPlatform.Windows,
                    OSPlatform.OSX,
                    OSPlatform.Linux
                }
            },
            new MakeThumbnailToolAbility
            {
                TypeOfTool = typeof(VLCTool),
                CanProcessFiles = new List<SupportedFileTypeEnum>
                {
                    SupportedFileTypeEnum.MP4,
                    SupportedFileTypeEnum.MOV,
                },
                SupportedPlatform = new List<OSPlatform>
                {
                    OSPlatform.Windows,
                    OSPlatform.OSX
                }
            },
            //new MakeThumbnailToolAbility
            //{
            //    TypeOfTool = typeof(DocNetTool),
            //    CanProcessFiles = new List<SupportedFileTypeEnum>
            //    {
            //        SupportedFileTypeEnum.PDF,
            //    },
            //    SupportedPlatform = new List<OSPlatform>
            //    {
            //        OSPlatform.Windows,
            //        OSPlatform.OSX,
            //        OSPlatform.Linux
            //    }
            //},
        };
        private static OSPlatform CurrentPlatform
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return OSPlatform.Windows;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return OSPlatform.OSX;
                }
                return OSPlatform.Linux;
            }
        }

        public static IMakeThumbnailTool GetResizingTool(string fileExtensionString, out SupportedFileTypeEnum? fileType)
        {
            var fileExtension = FileTypeHelper.String2FileType(fileExtensionString);
            if (!CanProcessFileType(fileExtension))
            {
                fileType = null;
                return null;
            }
            var tool = _tools
                            .First(t => t.CanProcessFiles.Any(c => c == fileExtension) &&
                                t.SupportedPlatform.Any(s => s == CurrentPlatform));
            fileType = fileExtension;

            return (IMakeThumbnailTool)Activator.CreateInstance(tool.TypeOfTool);
        }

        public static IMakeThumbnailTool GetResizingTool(string fileExtensionString)
        {
            var fileExtension = FileTypeHelper.String2FileType(fileExtensionString);
            if (!CanProcessFileType(fileExtension))
            {
                return null;
            }
            var tool = _tools
                            .First(t => t.CanProcessFiles.Any(c => c == fileExtension) &&
                                t.SupportedPlatform.Any(s => s == CurrentPlatform));

            return (IMakeThumbnailTool)Activator.CreateInstance(tool.TypeOfTool);
        }

        public static bool CanProcessFileType(SupportedFileTypeEnum fileExtension)
        {
            return _tools
                    .Any(t => t.CanProcessFiles.Any(c => c == fileExtension) &&
                                t.SupportedPlatform.Any(s => s == CurrentPlatform));
        }
    }
}
