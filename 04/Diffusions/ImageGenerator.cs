using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Diffusions {
  public abstract class ImageGenerator : IImageGenerator {
    public bool Finished { get; protected set; } = false;
    protected CancellationTokenSource cancellationTokenSource;

    public void Start(Area area) {
      cancellationTokenSource = new CancellationTokenSource();
      var token = cancellationTokenSource.Token;
      Task.Run(() =>
      {
        Finished = false;

        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; 
             i < Settings.Default.MaxIterations && !token.IsCancellationRequested; i++)
        {
          UpdateMatrix(area);

          if (i % Settings.Default.DisplayInterval == 0)
          {
            OnImageGenerated(area, ColorSchema.GenerateBitmap(area), sw.Elapsed);
          }
        }
        sw.Stop();
        Finished = true;
        OnImageGenerated(area, ColorSchema.GenerateBitmap(area), sw.Elapsed);
      }, token);
    }

    public void Stop() {
      cancellationTokenSource.Cancel();
    }

    protected abstract void UpdateMatrix(Area area);

    public event EventHandler<EventArgs<Tuple<Area, Bitmap, TimeSpan>>> ImageGenerated;
    protected void OnImageGenerated(Area area, Bitmap bitmap, TimeSpan timespan) {
      var handler = ImageGenerated;
      if (handler != null) handler(this, new EventArgs<Tuple<Area, Bitmap, TimeSpan>>(new Tuple<Area, Bitmap, TimeSpan>(area, bitmap, timespan)));
    }

  }
}
