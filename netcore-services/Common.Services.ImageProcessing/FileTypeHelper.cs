using System;
using System.IO;
using System.Linq;
using Common.Services.Static.Logger;

namespace Common.Services.ImageProcessing
{
    public enum SupportedFileTypeEnum
    {
        //COMMON (1 - 999)
        JPG = 1,
        JPEG = 2,
        PNG = 3,
        TIF = 7,
        TIFF = 8,

        //PSD
        PSD = 1000,

        //RAWS (1001 - 2500)
        //Canon (1001 - 1500)
        CR2 = 1001,
        CR3 = 1002,
        CRW = 1003,
        //Nikon (1501 - 1800)
        NEF = 1501,
        NRW = 1502,

        EPS = 1600,
        PDF = 1700,
        
        //Sony (1801-2500)
        ARW = 1801,

        //EIP
        EIP = 3000,

        //Video files
        MP4 = 5000,
        MOV = 5010,
    }
    public enum SupportedVideoCodecEnum
    {
        H264 = 1,
    }
    public class FileTypeHelper
    {
        public static bool IsInternetMediaTypeSupported(string internet_media_type)
        {
            return System.Enum.GetValues(typeof(SupportedVideoCodecEnum))
                                .Cast<SupportedVideoCodecEnum>()
                                .ToList()
                                .Exists(c => internet_media_type.Replace(".", "").ToUpper().Contains(c.ToString()));
        }

        public static bool IsSupported(string fileExtension)
        {
            return System.Enum.GetValues(typeof(SupportedFileTypeEnum))
                    .Cast<SupportedFileTypeEnum>()
                    .ToList()
                    .Exists(c => c.ToString() == fileExtension.Replace(".", "").ToUpper());
        }
        public static SupportedFileTypeEnum String2FileType(string fileExtension)
        {
            return System.Enum.GetValues(typeof(SupportedFileTypeEnum))
                    .Cast<SupportedFileTypeEnum>()
                    .ToList()
                    .FirstOrDefault(c => c.ToString() == fileExtension.Replace(".", "").ToUpper());
        }

        /// <summary>
        ///  JPG = 1,  JPEG = 2, PNG = 3, TIF = 7, TIFF = 8,
        /// </summary>
        public static bool IsCommonFormat(string filePathOrExtension)
        {
            return System.Enum.GetValues(typeof(SupportedFileTypeEnum))
                        .Cast<SupportedFileTypeEnum>()
                        .Where(t => (int)t < 1000)
                        .ToList()
                        .Any(c => c.ToString() == Path.GetExtension(filePathOrExtension).Replace(".", "").ToUpper());
        }

        public static bool IsPSDFormat(string filePathOrExtension)
        {
            return System.Enum.GetValues(typeof(SupportedFileTypeEnum))
                        .Cast<SupportedFileTypeEnum>()
                        .Where(t => t == SupportedFileTypeEnum.PSD)
                        .ToList()
                        .Any(c => c.ToString() == Path.GetExtension(filePathOrExtension).Replace(".", "").ToUpper());
        }

        public static bool IsRawFormat(string filePathOrExtension)
        {
            return System.Enum.GetValues(typeof(SupportedFileTypeEnum))
                        .Cast<SupportedFileTypeEnum>()
                        .Where(t => ((int)t >= 1000 && (int)t < 1600) || (int)t == 1801) 
                        .ToList()
                        .Any(c => c.ToString() == Path.GetExtension(filePathOrExtension).Replace(".", "").ToUpper());
        }

        public static bool IsImageFormats(string filePathOrExtension)
        {
            return System.Enum.GetValues(typeof(SupportedFileTypeEnum))
                        .Cast<SupportedFileTypeEnum>()
                        .Where(t => (int)t < 5000 && (int)t >= 1)
                        .ToList()
                        .Any(c => c.ToString() == Path.GetExtension(filePathOrExtension).Replace(".", "").ToUpper());
        }

        public static bool IsVideoFormats(string filePathOrExtension)
        {
            return System.Enum.GetValues(typeof(SupportedFileTypeEnum))
                        .Cast<SupportedFileTypeEnum>()
                        .Where(t => (int)t >= 5000)
                        .ToList()
                        .Any(c => c.ToString() == Path.GetExtension(filePathOrExtension).Replace(".", "").ToUpper());
        }

        public static bool IsEIPFormat(string filePathOrExtension)
        {
            return System.Enum.GetValues(typeof(SupportedFileTypeEnum))
                        .Cast<SupportedFileTypeEnum>()
                        .Where(t => t == SupportedFileTypeEnum.EIP)
                        .ToList()
                        .Any(c => c.ToString() == Path.GetExtension(filePathOrExtension).Replace(".", "").ToUpper());
        }

        public static bool IsNRWFormat(string filePathOrExtension)
        {
            return System.Enum.GetValues(typeof(SupportedFileTypeEnum))
                        .Cast<SupportedFileTypeEnum>()
                        .Where(t => t == SupportedFileTypeEnum.NRW)
                        .ToList()
                        .Any(c => c.ToString() == Path.GetExtension(filePathOrExtension).Replace(".", "").ToUpper());
        }
        
        public static bool IsARWFormat(string filePathOrExtension)
        {
            return System.Enum.GetValues(typeof(SupportedFileTypeEnum))
                .Cast<SupportedFileTypeEnum>()
                .Where(t => t == SupportedFileTypeEnum.ARW)
                .ToList()
                .Any(c => c.ToString() == Path.GetExtension(filePathOrExtension).Replace(".", "").ToUpper());
        }

        public static bool IsNEFFormat(string filePathOrExtension)
        {
            return System.Enum.GetValues(typeof(SupportedFileTypeEnum))
                        .Cast<SupportedFileTypeEnum>()
                        .Where(t => t == SupportedFileTypeEnum.NEF)
                        .ToList()
                        .Any(c => c.ToString() == Path.GetExtension(filePathOrExtension).Replace(".", "").ToUpper());
        }

        public static bool IsJPG(string filePath)
        {
            return Check(filePath, new byte[] { 0xFF, 0xD8, 0xFF });
        }
        public static bool IsPNG(string filePath)
        {
            return Check(filePath, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        }
        public static bool IsTIF(string filePath)
        {
            return Check(filePath, new byte[] { 0x49, 0x20, 0x49 }) ||
              Check(filePath, new byte[] { 0x49, 0x49, 0x2A, 0x00 }) ||
              Check(filePath, new byte[] { 0x4D, 0x4D, 0x00, 0x2A }) ||
              Check(filePath, new byte[] { 0x4D, 0x4D, 0x00, 0x2B });
        }
        public static bool IsPSD(string filePath)
        {
            return Check(filePath, new byte[] { 0x38, 0x42, 0x50, 0x53 });
        }
        public static bool IsCR2(string filePath)
        {
            return (Check(filePath, new byte[] { 0x49, 0x49, 0x2A, 0x0 }) || Check(filePath, new byte[] { 0x4D, 0x4D, 0x0, 0x2A }))
                   && Check(filePath, new byte[] { 0x43, 0x52 }, 8);
        }
        public static bool IsCR3(string filePath)
        {
            return Check(filePath, new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x63, 0x72, 0x78, 0x20, 0x00, 0x00, 0x00, 0x01, 0x63, 0x72, 0x78, 0x20, 0x69, 0x73, 0x6f, 0x6d });
        }
        public static bool IsCRW(string filePath)
        {
            return Check(filePath, new byte[] { 0x49, 0x49, 0x1A, 0x00, 0x00, 0x00, 0x48, 0x45, 0x41, 0x50, 0x43, 0x43, 0x44, 0x52, 0x02, 0x00 });
        }
        public static bool IsPDF(string filePath)
        {
            return Check(filePath, new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D });
        }
        public static bool IsMP4(string filePath)
        {
            return Check(filePath, new byte[] { 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D }, 4) ||
                      Check(filePath, new byte[] { 0x66, 0x74, 0x79, 0x70, 0x4D, 0x53, 0x4E, 0x56 }, 4) ||
                      Check(filePath, new byte[] { 0x66, 0x74, 0x79, 0x70, 0x58, 0x41, 0x56, 0x43 }, 4) ||
                      Check(filePath, new byte[] { 0x66, 0x74, 0x79, 0x70, 0x6D, 0x70, 0x34, 0x32 }, 4);
        }
        private static bool Check(string filePath, byte[] header, int offset = 0, byte[] mask = null)
        {
            if (!string.IsNullOrEmpty(filePath) && !File.Exists(filePath))
            {
                return false;
            }

            int minimumBytes = 100;

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[minimumBytes];
                fileStream.Read(buffer, 0, minimumBytes);
                for (int i = 0; i < header.Length; i++)
                {
                    // If a bitmask is set
                    if (mask != null)
                    {
                        // If header doesn't equal `buf` with bits masked off
                        if (header[i] != (mask[i] & buffer[i + offset]))
                        {
                            return false;
                        }
                    }
                    else if (header[i] != buffer[i + offset])
                    {
                        return false;
                    }
                }
                return true;
            }

        }
    }
}
