using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
// using Common.Services.Data.User;
using Common.Services.Static;
using Common.Services.Static.Logger;
using Common.Services.Core;
using Common.Services.Model.Uploader;
using Common.Services.Services;
using Common.Services.Static.Queue.Core;
using Microsoft.Extensions.Logging;

// using Common.Services.Model.Uploader.Enum;
// using Common.Services.Services.Uploader;
// using Common.Services.Uploader;

namespace Common.Services.Worker
{
  public class UploaderWorker : IServiceWorker
  {


    // Flag to track whether DoWork is currently processing
    private bool _isProcessing;
    private readonly IUploadService _uploadService;
    private readonly IQueue<UploadFileModel> _uploadQueue;

    public UploaderWorker(
      IUploadService uploadService,
      IQueue<UploadFileModel> queue
      )
    {
      _uploadService = uploadService;
      _uploadQueue = queue;
      queue.ProcessItem += UploadBatchFiles;

    }

    private string _versionInfo = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    public async Task DoWork()
    {
      if (_isProcessing)
      {
        Logger.Info("[TransferFlow][UploadWorker][DoWork] - Skipping DoWork since it is already running.");
        return;
      }

      _isProcessing = true; // Set the flag to indicate processing has started

      HandleSocketEvent();
    }

    private void HandleSocketEvent()
    {
      try
      {
        SocketIntegration.On(SocketEvents.UploadBatchFiles, socketData =>
        {
          ProcessSocketMessage(SocketEvents.UploadBatchFiles, socketData.ToString());
        });
      } catch (Exception ex)
      {
        Logger.Error(ex, $"[Uploader] Handle socket event error");
      }
    }

     private void ProcessSocketMessage(string @event, string receivedRawData)
        {
            if (string.IsNullOrEmpty(receivedRawData))
            {
                return;
            }

            if (@event == SocketEvents.UploadBatchFiles)
            {
                SocketUploadBatchFilesModel data =
                  Newtonsoft.Json.JsonConvert.DeserializeObject<SocketUploadBatchFilesModel>(receivedRawData);
                UploadFileModel uploadBatchFiles = new UploadFileModel
                {
                  ItemId = data.id,
                  Headers = data.headers.ToDictionary(header => header.key, header => header.value),
                  Files = data.files.ToDictionary(f => f.localId, v => new FileUploadModel
                  {
                    LocalId = v.localId,
                    LocalPath = v.localPath,
                    FileLength = v.fileLength,
                    PresignedUrl = v.presignedUrl
                  }  )
                };
                _uploadQueue.Push(uploadBatchFiles);
            }

            if (@event == SocketEvents.StopUpload)
            {
              SocketStopUploadBatchModel data = Newtonsoft.Json.JsonConvert.DeserializeObject<SocketStopUploadBatchModel>(receivedRawData);
              Logger.Warning($"[Uploader] Request cancel upload batch {data.itemId}");
              if (_uploadQueue != null)
              {
                Logger.Debug($"[Uploader] Cancel upload batch {data.itemId}");
                _uploadQueue.Remove(data.itemId);
              }
            }


        }

        private void UploadBatchFiles(UploadFileModel batch, CancellationToken cancelToken)
        {
          try
          {
            Logger.Info($"[Uploader] Start upload batch file {batch.ItemId}", batch);

          }
          catch (Exception ex)
          {
            Logger.Error(ex, $"[Uploader] Upload batch file {batch.ItemId} has exception");
            throw;
          }
        }
  }
}
