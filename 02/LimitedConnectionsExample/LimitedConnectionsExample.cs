using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualBasic.CompilerServices;

namespace LimitedConnectionsExample
{
    class LimitedConnectionsExample
    {
        public static void Main()
        {
            var l = new LimitedConnectionsExample();
            l.DownloadFilesAsync(new[]
                {"1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"});
            Console.WriteLine("Hello");
        }

        public void DownloadFilesAsync(IEnumerable<string> urls)
        {
            var downloadThread = new Thread(() =>
            {
                var sem = new Semaphore(10, 10);
                foreach (var url in urls)
                {
                    sem.WaitOne();
                    var thread = new Thread(() =>
                    {
                        DownloadFile(url);
                        sem.Release();
                    });
                    thread.Start();
                }
            });
            downloadThread.Start();
        }

        public void DownloadFiles(IEnumerable<string> urls)
        {
            ICollection<Thread> threads = new List<Thread>();
            foreach (var url in urls)
            {
                var thread = new Thread(() => DownloadFile(url));
                thread.Start();
                threads.Add(thread);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        private void DownloadFile(object url)
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Console.WriteLine(url);
            // download and store file ... 
        }
    }
}