using System;
using System.Collections.Generic;
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

        public async Task DoWork()
        {
            Console.WriteLine("DO work uploader");

            // Listen socket events
            SocketIntegration.On(SocketEvents.UploadBatchFiles, socketData =>
            {
              Console.WriteLine("[Uploader] Handler socket data", socketData);
              // Mock return data
              SocketIntegration.Send(SocketEvents.FinishUpload, "v1.0.0");
            });
        }


    }
}
