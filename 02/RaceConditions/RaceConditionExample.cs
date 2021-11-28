using System;
using System.Threading;

namespace RaceConditions
{
    class RaceConditionExample
    {
        private const int N = 1000;
        private const int BUFFER_SIZE = 10;

        private double[] buffer;
        private AutoResetEvent writerReady;
        private AutoResetEvent readerReady;

        public void Run()
        {
            buffer = new double[BUFFER_SIZE];
            writerReady = new AutoResetEvent(false);
            readerReady = new AutoResetEvent(false);

            // start threads 
            var t1 = new Thread(Reader);
            var t2 = new Thread(Writer);
            t1.Start();
            t2.Start();

            // wait for threads 
            t1.Join();
            t2.Join();
        }

        private void Reader()
        {
            var readerIndex = 0;
            for (int i = 0; i < N; i++)
            {
                readerReady.Set();
                writerReady.WaitOne();
                Console.WriteLine(buffer[readerIndex]);
                readerIndex = (readerIndex + 1) % BUFFER_SIZE;
            }
        }

        private void Writer()
        {
            var writerIndex = 0;
            for (int i = 0; i < N; i++)
            {
                readerReady.WaitOne();
                buffer[writerIndex] = i;
                writerReady.Set();
                writerIndex = (writerIndex + 1) % BUFFER_SIZE;
            }
        }
    }
}