using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Services.ImageProcessing.Model
{
    public enum OrientationMirrorType
    {
        NONE = 0,
        FLIP = 1,
        FLOP = 2,
    }
    public class OrientationElement
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public int RotateDegrees { get; set; }
        public OrientationMirrorType MirrorType { get; set; }
    }
    public static class OrientationName
    {
        public static OrientationElement GetById(int orientationId)
        {
            if (orientationId < 0 || orientationId > OrientationElements.Count)
            {
                return null;
            }
            return OrientationElements[orientationId - 1];
        }

        /**
         * https://exiftool.org/TagNames/EXIF.html
         *   1 = Horizontal (normal)
         *   2 = Mirror horizontal
         *   3 = Rotate 180
         *   4 = Mirror vertical
         *   5 = Mirror horizontal and rotate 270 CW
         *   6 = Rotate 90 CW
         *   7 = Mirror horizontal and rotate 90 CW
         *   8 = Rotate 270 CW
         * Flip image (reflect each scanline in the vertical direction).
         * Flop image (reflect each scanline in the horizontal direction).
         **/
        private static List<OrientationElement> OrientationElements => new List<OrientationElement>()
        {
            new OrientationElement
            {
                Id = 1,
                Description = "Horizontal (normal)",
                RotateDegrees = 0,
                MirrorType = OrientationMirrorType.NONE
            },
            new OrientationElement
            {
                Id = 2,
                Description = "Mirror horizontal",
                RotateDegrees = 0,
                MirrorType = OrientationMirrorType.FLOP
            },
            new OrientationElement
            {
                Id = 3,
                Description = "Rotate 180", 
                RotateDegrees = 360 - 180,
                MirrorType = OrientationMirrorType.NONE
            },
            new OrientationElement
            {
                Id = 4,
                Description = "Mirror vertical",
                RotateDegrees = 0,
                MirrorType = OrientationMirrorType.FLIP
            },
            new OrientationElement
            {
                Id = 5,
                Description = "Mirror horizontal and rotate 270 CW",
                RotateDegrees = 270,
                MirrorType = OrientationMirrorType.FLOP
            },
            new OrientationElement
            {
                Id = 6,
                Description = "Rotate 90 CW",
                RotateDegrees = 90,
                MirrorType = OrientationMirrorType.NONE
            },
            new OrientationElement
            {
                Id = 7,
                Description = "Mirror horizontal and rotate 90 CW",
                RotateDegrees = 90,
                MirrorType = OrientationMirrorType.FLOP
            },
            new OrientationElement
            {
                Id = 8,
                Description = "Rotate 270 CW",
                RotateDegrees = 270,
                MirrorType = OrientationMirrorType.NONE
            },
        };
    }
}
