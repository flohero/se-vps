using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace MandelbrotGenerator
{
    public class AsyncThreadImageGenerator : IImageGenerator
    {
        public event EventHandler<EventArgs<Tuple<Area, Bitmap, TimeSpan>>> ImageGenerated;
        private CancellationTokenSource cts;

        public void GenerateImage(Area area)
        {
            // Cancel previous calculations
            cts?.Cancel(false);
            cts = new CancellationTokenSource();
            var thread = new Thread(Run);
            thread.Start(new Tuple<Area, CancellationToken>(area, cts.Token));
        }

        private void Run(object obj)
        {
            var tuple = (Tuple<Area, CancellationToken>) obj;
            var area = tuple.Item1;
            var token = tuple.Item2;
            var sw = new Stopwatch();
            sw.Start();
            var bitmap = SyncImageGenerator.GenerateMandelbrotSet(area, token);
            sw.Stop();
            OnImageGenerated(area, bitmap, sw.Elapsed);
        }

        private void OnImageGenerated(Area area, Bitmap bitmap, TimeSpan elapsed)
        {
            var handler = ImageGenerated;
            handler?.Invoke(this,
                new EventArgs<Tuple<Area, Bitmap, TimeSpan>>(
                    new Tuple<Area, Bitmap, TimeSpan>(area, bitmap, elapsed)
                )
            );
        }
    }
}