using System;
using System.Drawing;

namespace MandelbrotGenerator {
  public interface IImageGenerator
  {
    event EventHandler<EventArgs<Tuple<Area, Bitmap, TimeSpan>>> ImageGenerated;
    void GenerateImage(Area area);
  }
}
