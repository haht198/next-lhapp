using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
// using Common.Services.Data.User;
// using Common.Services.Static;
// using Common.Services.Static.Logger;
using Common.Services.Core;
// using Common.Services.Model.Uploader.Enum;
// using Common.Services.Services.Uploader;
// using Common.Services.Uploader;

namespace Common.Services.Worker
{
    public class UploaderWorker : IServiceWorker
    {


        // Flag to track whether DoWork is currently processing
        private bool _isProcessing;

        public UploaderWorker()
        {

        }

        private string versionInfo =  Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        public async Task DoWork()
        {
            Console.WriteLine("DO work uploader");

            // Listen socket events
            SocketIntegration.On(SocketEvents.UploadBatchFiles, socketData =>
            {
              Console.WriteLine("[Uploader] Handler socket data", socketData);
              // Mock return data
              var data = new Dictionary<string, object>();
              data.Add("status", "OK");
              data.Add("message", $"FINISH_UPLOAD_RESULT_v{versionInfo}");
              data.Add("socketData", socketData);
              SocketIntegration.Send(SocketEvents.FinishUpload, data);
            });
        }


    }
}
