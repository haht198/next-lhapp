using System.Collections.Generic;

namespace Common.Services.Model.Uploader
{
    public class SocketUploadBatchFilesModel
    {
        public string id { get; set; }
        public List<PreSignedUrlHeader> headers { get; set; }
        public List<FileModel> files { get; set; }
    }

    public class PreSignedUrlHeader
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class FileModel
    {
        public string localId { get; set; }
        public string contentType { get; set; }
        public string localPath { get; set; }
        public string presignedUrl { get; set; }
        public long fileLength { get; set; }
    }

    public class SocketStopUploadBatchModel
    {
      public string itemId { get; set; }
    }
}
