using System;
using System.IO;

namespace PactTests
{
    public class TempFile : IDisposable
    {
        public TempFile()
        {
            Filename = Path.GetTempFileName();
        }

        public string Filename { get; }

        public void Dispose()
        {
            File.Delete(Filename);
        }
    }
}
