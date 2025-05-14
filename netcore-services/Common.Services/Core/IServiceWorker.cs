using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Common.Services.Core
{
    public interface IServiceWorker
    {
        Task DoWork();
    }
}
