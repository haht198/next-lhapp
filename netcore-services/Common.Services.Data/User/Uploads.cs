using System;
using System.Collections.Generic;

namespace Common.Services.Data.User
{
    public partial class Uploads
    {
        public long Id { get; set; }
        public string FilePath { get; set; }
        public int? Rating { get; set; }
        public string ThumbnailPath { get; set; }
        public long TransferId { get; set; }
        public long? StyleGuidePositionId { get; set; }
        public int? IsInclipFile { get; set; }
        public long Status { get; set; }
        public long CreatedDateTimeUtc { get; set; }
        public long? StartedDateTimeUtc { get; set; }
        public long? FinishedDateTimeUtc { get; set; }
    }
}
