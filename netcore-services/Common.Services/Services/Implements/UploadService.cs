using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading;
using System.Threading.Tasks;
using Common.Services.Model.Uploader;
using Common.Services.Static;
using Common.Services.Static.Logger;

namespace Common.Services.Services.Implements
{
  public class UploadService: IUploadService
  {
    public bool UploadFileAsync(dynamic input, Dictionary<string, string> customHeaders = null,
      Action<UploadProgressModel> progress = null, CancellationToken cancelToken = default)
    {
      Logger.Info($"[UploadService] UploadFileAsync {input.FileLocalPath}");

      var (handler, client) = GetHttpClientWithHandler();
       try
            {
                using (var stream = File.OpenRead(input.FileLocalPath))
                using (var request = new HttpRequestMessage(HttpMethod.Put, input.FileUrl))
                using (var content = new StreamContent(stream))
                {
                    if (customHeaders != null && customHeaders.Any())
                    {
                        foreach (var customHeader in customHeaders)
                        {
                            client.DefaultRequestHeaders.TryAddWithoutValidation(customHeader.Key, customHeader.Value);
                        }
                    }
                    if (!string.IsNullOrEmpty(input.ContentType))
                    {
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(input.ContentType);
                    }
                    var tokenSource = new CancellationTokenSource();
                    tokenSource.CancelAfter(TimeSpan.FromMilliseconds(System.Threading.Timeout.Infinite));

                    handler.HttpSendProgress += (s, e) =>
                    {
                        if (progress != null)
                        {
                            progress.Invoke(new UploadProgressModel
                            {
                              UploadedFileSize = e.BytesTransferred
                            });
                        }
                    };
                    if (cancelToken != null)
                    {
                        cancelToken.Register(() =>
                        {
                            try
                            {
                                client.CancelPendingRequests();
                            }
                            catch (ObjectDisposedException e)
                            {
                                //object has been disposed
                            }
                        });
                    }
                    request.Content = content;
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Common Service Uploader");
                    try
                    {
                        var resonse = client.SendAsync(request).GetAwaiter().GetResult();
                        Logger.Debug($"[Common.Service.Uploader][UploadService] Upload file {input.FileLocalPath} to s3 response status {resonse.StatusCode}", resonse);
                        if (!resonse.IsSuccessStatusCode)
                        {
                            Logger.Error(
                                $"[Common.Service.Uploader][UploadService] Upload file {input.FileLocalPath} to s3 failed.");
                            return false;
                        }

                        return true;
                    }
                    catch (HttpRequestException ex) {
                        // Xử lý lỗi network
                        Logger.Error($"[Hue.Service.Uploader][UploadService] Upload file {input.FileLocalPath} to s3 has a HttpRequestException {ex.Message}", ex);
                        throw;
                    } catch (TaskCanceledException ex) {
                        // Xử lý lỗi timeout
                        Logger.Error($"[Hue.Service.Uploader][UploadService] Upload file {input.FileLocalPath} to s3 a TaskCanceledException {ex.Message}", ex);
                        throw;
                    } catch (Exception ex) {
                        // Xử lý các lỗi khác
                        Logger.Error($"[Hue.Service.Uploader][UploadService] Upload file {input.FileLocalPath} to s3 a Exception {ex.Message}", ex);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                var errmsg = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
                Logger.Error(ex, $"Upload file error [UploadFileAsyncUsingHttpClient]: {input.FileLocalPath}", input);
                throw;
            }
            finally
            {
                if (client != null) client.Dispose();
                if (handler != null) handler.Dispose();
            }

    }

    private (ProgressMessageHandler, HttpClient) GetHttpClientWithHandler()
    {
      var handler = new HttpClientHandler();

      if (ProgramArguments.Proxy != null)
      {
        handler.Proxy = ProgramArguments.Proxy;
        handler.PreAuthenticate = true;
        handler.UseDefaultCredentials = false;
      }
      var progressHandler = new ProgressMessageHandler(handler);

      return (progressHandler, new HttpClient(progressHandler)
      {
        Timeout = TimeSpan.FromMilliseconds(System.Threading.Timeout.Infinite)
      });
    }
  }
}
