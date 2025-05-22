using System;
using System.Collections.Generic;
using System.Threading;
using Common.Services.Model.Uploader;
using Common.Services.Static.Logger;

namespace Common.Services.Services.Implements
{
  public class UploadService: IUploadService
  {
    public bool UploadFileAsync(dynamic input, Dictionary<string, string> customHeaders = null,
      Action<UploadProgressModel> progress = null, CancellationToken cancelToken = default)
    {
      Logger.Info($"[UploadService] UploadFileAsync {input.FileLocalPath}");
      return true;
    }
  }
}
