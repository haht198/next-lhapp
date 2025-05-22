using System.Collections.Generic;
using Common.Services.Static.Queue.Core;

namespace Common.Services.Model.Uploader
{
  public class UploadFileModel : IQueueItem
  {
    public string ItemId { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public Dictionary<string, FileUploadModel> Files { get; set; }
  }

  public class UploadRequestHeader
  {
    public string Key { get; set; }
    public string Value { get; set; }
  }

  public class FileUploadModel
  {
    public string LocalId { get; set; }
    public string ContentType { get; set; }
    public string LocalPath { get; set; }
    public string PresignedUrl { get; set; }
    public long FileLength { get; set; }
  }


  public class UploadProgressModel
  {
    public long UploadedFileSize { get; set; }
  }
}
