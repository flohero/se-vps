using System;
using System.Threading;

namespace RaceConditions
{
    class RaceConditionExample
    {
        private const int N = 1000;
        private const int BUFFER_SIZE = 10;

        private double[] buffer;
        private Semaphore _full;
        private Semaphore _empty;

        public void Run()
        {
            buffer = new double[BUFFER_SIZE];
            _full = new Semaphore(0, BUFFER_SIZE);
            _empty = new Semaphore(BUFFER_SIZE, BUFFER_SIZE);

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
                _full.WaitOne();
                lock (buffer)
                    Console.WriteLine(buffer[readerIndex]);
                _empty.Release();
                readerIndex = (readerIndex + 1) % BUFFER_SIZE;
            }
        }

        private void Writer()
        {
            var writerIndex = 0;
            for (int i = 0; i < N; i++)
            {
                _empty.WaitOne();
                lock (buffer)
                    buffer[writerIndex] = i;
                _full.Release();
                writerIndex = (writerIndex + 1) % BUFFER_SIZE;
            }
        }
    }
}