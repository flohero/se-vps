using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace MandelbrotGenerator
{
    public class SyncImageGenerator : IImageGenerator
    {
        public event EventHandler<EventArgs<Tuple<Area, Bitmap, TimeSpan>>> ImageGenerated;
        private CancellationTokenSource cts;

        public void GenerateImage(Area area)
        {
            cts?.Cancel(false);

            cts = new CancellationTokenSource();
            var sw = new Stopwatch();
            sw.Start();
            var bitmap = SyncImageGenerator.GenerateMandelbrotSet(area, cts.Token);
            sw.Stop();

            if (bitmap != null)
            {
                OnImageGenerated(area, bitmap, sw.Elapsed);
            }
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

        public static Bitmap GenerateMandelbrotSet(Area area, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }

            int maxIterations;
            double zBorder;
            double cReal, cImg, zReal, zImg, zNewReal, zNewImg;

            maxIterations = Settings.DefaultSettings.MaxIterations;
            zBorder = Settings.DefaultSettings.ZBorder * Settings.DefaultSettings.ZBorder;

            Bitmap bitmap = new Bitmap(area.Width, area.Height);

            for (var i = 0; i < area.Width; i++)
            {
                for (var j = 0; j < area.Height; j++)
                {
                    cReal = area.MinReal + i * area.PixelWidth; // extract starting points based on the grid position
                    cImg = area.MinImg + j * area.PixelHeight;
                    zReal = 0; // sequence variable = current value
                    zImg = 0;

                    var k = 0;
                    while (zReal * zReal + zImg * zImg < zBorder && k < maxIterations)
                    {
                        zNewReal = zReal * zReal - zImg * zImg + cReal;
                        zNewImg = 2 * zReal * zImg + cImg;
                        zReal = zNewReal;
                        zImg = zNewImg;
                        k++;
                    }

                    if (token.IsCancellationRequested)
                    {
                        return null;
                    }

                    bitmap.SetPixel(i, j, ColorSchema.GetColor(k));
                }
            }

            return bitmap;
        }
    }
}