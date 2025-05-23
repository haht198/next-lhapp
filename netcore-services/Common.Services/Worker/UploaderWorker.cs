using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
      queue.ProcessItem += UploadBatchFilesProcess;

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


        private void UploadBatchFilesProcess(UploadFileModel batch, CancellationToken cancelToken)
        {
          bool getPresignUrlSuccess = true;
          ConcurrentDictionary<string, UploadFileDoneModel> tempAssetDictionary = new ConcurrentDictionary<string, UploadFileDoneModel>();
          var uploadBatchFile = new UploadBatchFileModel()
          {
            ItemId = batch.ItemId,
            Files = new List<FileInputModel>(),
            Headers = new ConcurrentDictionary<string, string>(),
          };

          foreach (var keyValuePair in batch.Files.ToList())
          {
            uploadBatchFile.Files.Add(new FileInputModel
            {
              FileUrl = keyValuePair.Value.PresignedUrl,
              FileLocalPath = keyValuePair.Value.LocalPath,
              ContentType = keyValuePair.Value.ContentType
            });
          }

          foreach (var keyValuePair in batch.Headers)
          {
            uploadBatchFile.Headers.TryAdd(keyValuePair.Key, keyValuePair.Value);
          }
          var uploadBatchResult = UploadBatch(uploadBatchFile, cancelToken);
          Logger.Info($"[Uploader] Upload batch file {batch.ItemId} done", uploadBatchResult);
        }

        private UploadBatchResultModel UploadBatch(UploadBatchFileModel batch, CancellationToken cancelToken)
        {
          var uploadBatchResult = new UploadBatchResultModel
          {
            isSuccess = false,
            totalFileSize = 0,
            totalFileUploaded = 0,
            itemId = batch.ItemId,
          };
          try
          {
            Logger.Info($"[Uploader] Start upload batch file {batch.ItemId}", batch);


            var totalBatchSize = batch.Files.Sum(t => new FileInfo(t.FileLocalPath).Length);
            long uploadedSize = 0;
            double previousCallbackProgressPercent = 0;
            long previousUploadedBatchSize = 0;
            var previousNotifyBatchTime = DateTime.UtcNow;
            var interval = new Interval(() =>
            {
              if (totalBatchSize > 0)
              {
                var percent = Math.Round(uploadedSize * 100.0 / totalBatchSize, 2);
                var uploadedSpeed = (uploadedSize - previousUploadedBatchSize) /
                                    (DateTime.UtcNow - previousNotifyBatchTime).TotalSeconds /
                                    1024; // KBytes per second
                if (percent != previousCallbackProgressPercent)
                {
                  NotifyUploadProgress(batch.ItemId, percent, uploadedSpeed);
                  previousCallbackProgressPercent = percent;
                  previousUploadedBatchSize = uploadedSize;
                  previousNotifyBatchTime = DateTime.UtcNow;
                }
              }
            }, 2000).Run();

            //  Upload
            var notExistedFiles = batch.Files.Where(t => !File.Exists(t.FileLocalPath)).ToList();
            if (notExistedFiles.Count > 0)
            {
              Logger.Warning($"Some files of batch dose not existed", batch, notExistedFiles);
              uploadBatchResult.message =
                $"Missing files: \n{string.Join("\n", notExistedFiles.Select(t => t.FileLocalPath))}";

              return uploadBatchResult;
            }

            // Update result
            uploadBatchResult.isSuccess = true;
            using (var requireLock = new LockingFiles(batch.Files.Select(t => t.FileLocalPath)))
              Parallel.ForEach(batch.Files, new ParallelOptions() { MaxDegreeOfParallelism = 1, CancellationToken = cancelToken }, file =>
              {
                if (!cancelToken.IsCancellationRequested && uploadBatchResult.isSuccess)
                {
                    if (!File.Exists(file.FileLocalPath))
                    {
                        Logger.Error($"Upload file error due to file {file.FileLocalPath} does not exist", batch, file);
                        uploadBatchResult.isSuccess = false;
                        uploadBatchResult.message = $"File does not exist {file.FileLocalPath}";
                    }
                    var fileInfo = new FileInfo(file.FileLocalPath);

                    long temporaryUploadedSize = 0;
                    try
                    {
                        var uploadHeaders = batch.Headers.ToDictionary(t => t.Key, t => t.Value);
                        if (!string.IsNullOrEmpty(file.ContentType))
                        {
                            uploadHeaders.Add("Content-Type", file.ContentType);
                        }
                        var result = UploadFileAsync(file, uploadHeaders,
                                    (progress) =>
                                    {
                                        if (cancelToken == null || !cancelToken.IsCancellationRequested)
                                        {
                                            uploadedSize += (progress - temporaryUploadedSize);
                                            temporaryUploadedSize = progress;
                                        }
                                    }, cancelToken);
                        if (cancelToken != null && cancelToken.IsCancellationRequested)
                        {
                            return;
                        }
                        if (!result)
                        {
                            Logger.Error($"Upload file error {batch.ItemId} (unknow error)", batch, file);
                            uploadBatchResult.isSuccess = false;
                            uploadBatchResult.message = $"Unknow error"; // TODO detect error message
                        }
                        else
                        {
                          uploadBatchResult.totalFileUploaded++;
                          uploadBatchResult.totalFileSize += fileInfo.Length;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error when uploading file {batch.ItemId}", ex, batch, file);
                        uploadBatchResult.isSuccess = false;
                        uploadBatchResult.message = ex.Message;
                    }
                    finally
                    {
                        var filename = Path.GetFileNameWithoutExtension(file.FileLocalPath);
                        Logger.Tracing("CommonService", "UPLOAD")
                            .Info($"Upload file completed {file.FileLocalPath}|{filename}|{fileInfo.Extension}|{fileInfo.Length}");
                    }
                }
              });

            return uploadBatchResult;


          }
          catch (Exception ex)
          {
            Logger.Error(ex, $"[Uploader] Upload batch file {batch.ItemId} has exception");
            return uploadBatchResult;
          }
        }

        private bool UploadFileAsync(FileInputModel file, Dictionary<string, string> headers, Action<long> progressPercent = null, CancellationToken cancelToken = default, int retry = 1)
        {
          try
          {
            var isSuccess = _uploadService.UploadFileAsync(file, headers,
              (uploadProcessData) =>
              {
                if (progressPercent != null)
                {
                  progressPercent.Invoke(uploadProcessData.UploadedFileSize);
                }
              }, cancelToken);
            return isSuccess;
          }
          catch (Exception webex)
          {
            if (retry <= 3)
            {
              Logger.Warning($"Retry uploading file, retry time: {retry}", webex, file);
              Task.Delay(3000).Wait();
              return UploadFileAsync(file, headers, progressPercent, cancelToken, retry + 1);
            }
            else
            {
              throw;
            }
          }
        }

        private void NotifyUploadProgress(string itemId, double percent, double speed)
        {
          var uploadedPercent = percent <= 100 ? percent : 100;
          UploadNotify(SocketEvents.UploadProgress, new
          {
            itemId = itemId,
            uploadedPercent = (int)uploadedPercent,
            uploadedSpeed = speed
          });
        }

        private void UploadNotify(string @event, object data)
        {
          SocketIntegration.Send(@event, data);
        }
  }
}
