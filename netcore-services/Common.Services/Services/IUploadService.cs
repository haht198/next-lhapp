using System;
using System.Collections.Generic;
using System.Threading;
using Common.Services.Model.Uploader;

namespace Common.Services.Services
{
  public interface IUploadService
  {
    bool UploadFileAsync(dynamic input, Dictionary<string, string> customHeaders = null, Action<UploadProgressModel> progress = null, CancellationToken cancelToken = default);
  }
}
