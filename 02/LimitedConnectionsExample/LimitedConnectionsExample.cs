using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LimitedConnectionsExample
{
    class LimitedConnectionsExample
    {
        public static void Main()
        {
            var l = new LimitedConnectionsExample();
            l.DownloadFiles(new[]
                {"1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"});
        }

        public void DownloadFilesAsync(IEnumerable<string> urls)
        {
            if (!ThreadPool.SetMaxThreads(10, 10))
                throw new Exception("Cannot set min/max threads");
            foreach (var url in urls)
            {
                ThreadPool.QueueUserWorkItem(DownloadFile, url);
            }
        }

        public void DownloadFiles(IEnumerable<string> urls)
        {
            var toProcess = urls.Count();
            using ManualResetEvent resetEvent = new ManualResetEvent(false);
            foreach (var url in urls)
            {
                ThreadPool.QueueUserWorkItem(x =>
                {
                    DownloadFile(x);
                    if (Interlocked.Decrement(ref toProcess) == 0)
                        resetEvent.Set();
                }, url);
            }

            resetEvent.WaitOne();
        }

        private void DownloadFile(object url)
        {
            // download and store file ... 
        }
    }
}