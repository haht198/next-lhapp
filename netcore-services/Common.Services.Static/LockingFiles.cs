using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common.Services.Static
{
    public class LockingFiles : IDisposable
    {
        private Dictionary<string, FileStream> _lockedFiles;

        public LockingFiles(IEnumerable<string> files)
        {
            _lockedFiles = new Dictionary<string, FileStream>();
            foreach (var file in files)
            {
                if (!_lockedFiles.Keys.Any(t => t.Replace("\\", "/") == file.Replace("\\", "/")))
                {
                    _lockedFiles.Add(file, File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read));
                }
            }
        }
        public void Dispose()
        {
          try
          {
            foreach (var file in _lockedFiles)
            {
              if (file.Value != null)
              {
                file.Value.Close();
                file.Value.Dispose();
              }
            }

            _lockedFiles = new Dictionary<string, FileStream>();
          }
          catch (Exception)
          {
            Console.WriteLine("Error dispose locking files");
          }
        }
    }
}
