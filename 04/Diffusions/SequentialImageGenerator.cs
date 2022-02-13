namespace Diffusions
{
    public class SequentialImageGenerator : ImageGenerator
    {

        protected override void UpdateMatrix(Area area)
        {
            lock (area.Matrix)
            {
                var m = area.Matrix;

                for (var x = 0; x < area.Width; x++)
                {
                    var pX = (x + area.Width - 1) % area.Width;
                    var nX = (x + 1) % area.Width;

                    for (var y = 0; y < area.Height; y++)
                    {
                        var pY = (y + area.Height - 1) % area.Height;
                        var nY = (y + 1) % area.Height;
                        
                        area.NextMatrix[x, y] = (
                            m[pX, pY] + m[pX, y] + m[pX, nY] +
                            m[x, pY] + m[x, nY] +
                            m[nX, pY] + m[nX, y] + m[nX, nY]) / 8;
                    }
                }

                area.Matrix = area.NextMatrix;
                area.NextMatrix = m;
            }
        }
    }
}