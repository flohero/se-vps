using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace MandelbrotGenerator
{
    public class BackgroundWorkerImageGenerator : IImageGenerator
    {
        public event EventHandler<EventArgs<Tuple<Area, Bitmap, TimeSpan>>> ImageGenerated;
        private BackgroundWorker backgroundWorker;

        public BackgroundWorkerImageGenerator()
        {
            InitializeBackgroundWorker();
        }

        public void GenerateImage(Area area)
        {
            // Cancel previous calculations
            if (backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
                InitializeBackgroundWorker();
            }

            backgroundWorker.RunWorkerAsync(area);
        }

        private void Run(object sender, DoWorkEventArgs e)
        {
            var area = (Area) e.Argument;
            var sw = new Stopwatch();
            sw.Start();
            var worker = sender as BackgroundWorker;
            var bitmap = GenerateMandelbrotSet(area, worker, e);
            sw.Stop();
            e.Result = new Tuple<Area, Bitmap, TimeSpan>(area, bitmap, sw.Elapsed);
        }

        private static Bitmap GenerateMandelbrotSet(Area area, BackgroundWorker worker, DoWorkEventArgs e)
        {
            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return null;
            }

            int maxIterations;
            double zBorder;
            double cReal, cImg, zReal, zImg, zNewReal, zNewImg;

            maxIterations = Settings.DefaultSettings.MaxIterations;
            zBorder = Settings.DefaultSettings.ZBorder * Settings.DefaultSettings.ZBorder;

            var bitmap = new Bitmap(area.Width, area.Height);

            for (var i = 0; i < area.Width; i++)
            {
                for (var j = 0; j < area.Height; j++)
                {
                    cReal = area.MinReal + i * area.PixelWidth; // extract starting points based on the grid position
                    cImg = area.MinImg + j * area.PixelWidth;
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

                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        return null;
                    }

                    bitmap.SetPixel(i, j, ColorSchema.GetColor(k));
                }
            }

            return bitmap;
        }

        private void InitializeBackgroundWorker()
        {
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += Run;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.RunWorkerCompleted += OnImageGenerated;
        }

        private void OnImageGenerated(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                return;
            }

            var tuple = (Tuple<Area, Bitmap, TimeSpan>) e.Result;
            var handler = ImageGenerated;
            handler?.Invoke(this, new EventArgs<Tuple<Area, Bitmap, TimeSpan>>(tuple));
        }
    }
}