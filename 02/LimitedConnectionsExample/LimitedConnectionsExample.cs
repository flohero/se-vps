using System.Collections.Generic;
using System.Threading;

namespace LimitedConnectionsExample
{
    class LimitedConnectionsExample
    {
        public static void Main()
        {
            var l = new LimitedConnectionsExample();
        }

        public void DownloadFilesAsync(IEnumerable<string> urls)
        {
            var threads = new List<Thread>();
            foreach (var url in urls)
            {
                if (threads.Count == 10)
                {
                    // Wait for all 10 files to finish downloading, then start downloading remaining files
                    threads.ForEach(t => t.Join());
                    threads.Clear();
                }

                var t = new Thread(DownloadFile);
                t.Start(url);
                threads.Add(t);
            }
        }

        public void DownloadFiles(IEnumerable<string> urls)
        {
            var threads = new List<Thread>();
            foreach (var url in urls)
            {
                var t = new Thread(DownloadFile);
                t.Start(url);
                threads.Add(t);
            }
            // Wait for all files to finish downloading
            threads.ForEach(t => t.Join());
        }

        private void DownloadFile(object url)
        {
            // download and store file ... 
        }
    }
}