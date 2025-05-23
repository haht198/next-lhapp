using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Common.Services.Model.Uploader
{
  public class UploadBatchFileModel
  {
    public string ItemId { get; set; }
    public ConcurrentDictionary<string, string> Headers { get; set; }
    public List<FileInputModel> Files { get; set; }
  }

  public class FileInputModel
  {
    public string FileUrl { get; set; }
    public string FileLocalPath { get; set; }
    public string ContentType { get; set; }
  }


  public class UploadFileDoneModel  {
    public string localId { get; set; }
    public string tempAssetId { get; set; }
    public string tempFileId { get; set; }
  }


  public class UploadBatchResultModel
  {
    public string itemId { get; set; }
    public bool isSuccess { get; set; }
    public string message { get; set; }
    public int totalFileUploaded { get; set; }
    public long totalFileSize { get; set; }
  }
}
