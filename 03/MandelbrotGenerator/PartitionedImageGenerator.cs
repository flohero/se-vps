using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace MandelbrotGenerator
{
    public class PartitionedImageGenerator : IImageGenerator
    {
        public event EventHandler<EventArgs<Tuple<Area, Bitmap, TimeSpan>>> ImageGenerated;
        private CancellationTokenSource cts;
        private readonly int cols;
        private readonly int rows;
        private Bitmap[] bitmaps;

        public PartitionedImageGenerator()
        {
            var settings = Settings.DefaultSettings;
            cols = settings.Workers;
            rows = 1; // keep the possibility to use multiple rows
        }

        public void GenerateImage(Area area)
        {
            // Cancel previous calculations
            cts?.Cancel(false);
            cts = new CancellationTokenSource();
            var fractionWidth = (int) Math.Floor((double) area.Width / cols);
            var fractionHeight = (int) Math.Floor((double) area.Height / rows);
            var areas = (int) Math.Ceiling((double) area.Width / fractionWidth) *
                        (int) Math.Ceiling((double) area.Height / fractionHeight);
            bitmaps = new Bitmap[areas];
            var index = 0;
            for (var i = 0; i * fractionWidth < area.Width; i++)
            {
                var startWidth = i * fractionWidth;
                var endWidth = startWidth + fractionWidth > area.Width ? area.Width : startWidth + fractionWidth;
                for (var j = 0; j * fractionHeight < area.Height; j++)
                {
                    var startHeight = j * fractionHeight;
                    var endHeight = startHeight + fractionHeight > area.Height
                        ? area.Height
                        : startHeight + fractionHeight;
                    var thread = new Thread(Run);
                    thread.Start(new Tuple<Area, int, int, int, int, int, CancellationToken>(area, startWidth, endWidth,
                        startHeight, endHeight, index, cts.Token));
                    index++;
                }
            }
        }

        private void Run(object obj)
        {
            var tuple = (Tuple<Area, int, int, int, int, int, CancellationToken>) obj;
            var area = tuple.Item1;
            var index = tuple.Item6;
            var token = tuple.Item7;
            var sw = new Stopwatch();
            sw.Start();
            var bitmap = GenerateMandelbrotSetPart(area, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, token);
            sw.Stop();
            Console.WriteLine($"{index} took {sw.Elapsed}");
            OnImageGenerated(area, bitmap, sw.Elapsed, index);
        }

        private static Bitmap GenerateMandelbrotSetPart(Area area, int startWidth, int endWidth, int startHeight,
            int endHeight, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }

            var bitmap = new Bitmap(endWidth - startWidth, endHeight - startHeight);
            var maxIterations = Settings.DefaultSettings.MaxIterations;
            var zBorder = Settings.DefaultSettings.ZBorder * Settings.DefaultSettings.ZBorder;
            double cReal, cImg, zReal, zImg, zNewReal, zNewImg;

            for (var i = 0; i < bitmap.Width; i++)
            {
                for (var j = 0; j < bitmap.Height; j++)
                {
                    cReal = area.MinReal +
                            (i + startWidth) * area.PixelWidth; // extract starting points based on the grid position
                    cImg = area.MinImg + (j + startHeight) * area.PixelHeight;
                    zReal = 0.0;
                    zImg = 0.0;
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

        private void OnImageGenerated(Area area, Bitmap bitmap, TimeSpan elapsed, int index)
        {
            bitmaps[index] = bitmap;
            if (bitmaps.Any(map => map == null))
            {
                return;
            }

            var resultingBitmap = MergeBitmaps(area);
            var handler = ImageGenerated;
            handler?.Invoke(this,
                new EventArgs<Tuple<Area, Bitmap, TimeSpan>>(
                    new Tuple<Area, Bitmap, TimeSpan>(area, resultingBitmap, elapsed)
                )
            );
        }

        private Bitmap MergeBitmaps(Area area)
        {
            var result = new Bitmap(area.Width, area.Height);
            using (var g = Graphics.FromImage(result))
            {
                var startWidth = 0;
                var startHeight = 0;
                foreach (var t in bitmaps)
                {
                    g.DrawImage(t, startWidth, startHeight);
                    startHeight += t.Height;
                    if (startHeight < area.Height) continue;
                    startHeight = 0;
                    startWidth += t.Width;
                }
            }

            return result;
        }
    }
}